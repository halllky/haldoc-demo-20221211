using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.EFCore;
using HalApplicationBuilder.CodeRendering.ReactAndWebApi;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static HalApplicationBuilder.HalappProject;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace HalApplicationBuilder {
    public class HalappProject {

        private protected const string HALAPP_XML_NAME = "halapp.xml";
        private protected const string REACT_DIR = "ClientApp";
        internal const string REACT_PAGE_DIR = "pages";
        private protected const string HALAPP_DLL_COPY_TARGET = "halapp-resource";

        /// <summary>
        /// 新しいhalappプロジェクトを作成します。
        /// </summary>
        /// <param name="applicationName">アプリケーション名</param>
        /// <param name="verbose">ログの詳細出力を行うかどうか</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static HalappProject Create(string projectRootDir, string? applicationName, bool keepTempIferror, CancellationToken? cancellationToken = null, TextWriter? log = null, bool verbose = false) {

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new InvalidOperationException($"Please specify name of new application. example 'halapp create my-new-app'");

            if (Path.GetInvalidFileNameChars().Any(applicationName.Contains))
                throw new InvalidOperationException($"'{applicationName}' contains invalid characters for a file name.");

            if (Directory.Exists(projectRootDir))
                throw new InvalidOperationException($"'{projectRootDir}' is already exists.");

            var tempDir = keepTempIferror
                ? projectRootDir
                : Directory.CreateTempSubdirectory("halapp.temp.").FullName;

            var error = false;
            try {
                var tempProject = new HalappProject2(tempDir, cancellationToken, log, verbose);

                Directory.CreateDirectory(tempDir);

                tempProject.EnsureCreateHalappXml(applicationName);

                tempProject.DotnetNew();

                tempProject.EditProgramCs();
                tempProject.UpdateAutoGeneratedCode();

                tempProject.AddNugetPackages();
                tempProject.AddReferenceToHalappDll();

                tempProject.EnsureCreateDatabase();

                // git initial commit
                var cmd = new Cmd {
                    WorkingDirectory = tempDir,
                    CancellationToken = cancellationToken,
                    Verbose = verbose,
                };
                cmd.Exec("git", "init");
                cmd.Exec("git", "add", ".");
                cmd.Exec("git", "commit", "-m", "init");

                // ここまでの処理がすべて成功したら一時ディレクトリを本来のディレクトリ名に変更
                if (Directory.Exists(projectRootDir)) throw new InvalidOperationException($"プロジェクトディレクトリを {projectRootDir} に移動できません。");
                Directory.Move(tempDir, projectRootDir);

                log?.WriteLine("プロジェクト作成完了");

                return new HalappProject2(projectRootDir, cancellationToken, log, verbose);

            } catch {
                error = true;
                throw;

            } finally {
                if (Directory.Exists(tempDir) && (keepTempIferror == false || error == false)) {
                    try {
                        Directory.Delete(tempDir, true);
                    } catch (Exception ex) {
                        log?.WriteLine(new Exception("Failure to delete temp directory.", ex).ToString());
                    }
                }
            }
        }
        /// <summary>
        /// 既存のhalappプロジェクトを開きます。
        /// </summary>
        /// <param name="path">プロジェクトルートディレクトリの絶対パス</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static HalappProject Open(string? path, CancellationToken? cancellationToken = null, TextWriter? log = null, bool verbose = false) {
            if (string.IsNullOrWhiteSpace(path))
                return new HalappProject2(Directory.GetCurrentDirectory(), cancellationToken, log, verbose);
            else if (Path.IsPathRooted(path))
                return new HalappProject2(path, cancellationToken, log, verbose);
            else
                return new HalappProject2(Path.Combine(Directory.GetCurrentDirectory(), path), cancellationToken, log, verbose);
        }

        private protected HalappProject(string projetctRoot, CancellationToken? cancellationToken, TextWriter? log, bool verbose) {
            if (string.IsNullOrWhiteSpace(projetctRoot))
                throw new ArgumentException($"'{nameof(projetctRoot)}' is required.");

            ProjectRoot = projetctRoot;
            _cancellationToken = cancellationToken;
            _log = log;
            _verbose = verbose;
            _cmd = new Cmd {
                WorkingDirectory = projetctRoot,
                CancellationToken = cancellationToken,
                Verbose = _verbose,
            };
        }

        protected readonly bool _verbose;
        protected readonly CancellationToken? _cancellationToken;
        protected readonly TextWriter? _log;
        private protected readonly Cmd _cmd;

        private protected string ProjectRoot { get; }
        private protected Config ReadConfig() {
            var xmlFullPath = GetAggregateSchemaPath();
            using var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            var config = Core.Config.FromXml(xDocument);
            return config;
        }

        public string GetAggregateSchemaPath() {
            return Path.Combine(ProjectRoot, HALAPP_XML_NAME);
        }

        /// <summary>
        /// このディレクトリがhalappのものとして妥当なものかどうかを検査します。
        /// </summary>
        public bool IsValidDirectory() {
            var errors = new List<string>();

            if (Path.GetInvalidPathChars().Any(ProjectRoot.Contains))
                errors.Add($"Invalid path format: '{ProjectRoot}'");

            if (!Directory.Exists(ProjectRoot))
                errors.Add($"Directory '{ProjectRoot}' is not exist.");

            var halappXml = GetAggregateSchemaPath();
            if (!File.Exists(halappXml))
                errors.Add($"'{halappXml}' is not found.");

            if (_log != null) {
                foreach (var error in errors) _log.WriteLine(error);
            }
            return errors.Count == 0;
        }

        /// <summary>
        /// dotnet new コマンドを実行します。
        /// </summary>
        internal HalappProject DotnetNew() {
            _log?.WriteLine($"プロジェクトを作成します。");

            var config = ReadConfig();
            _cmd.Exec("dotnet", "new", "webapi", "--output", ".", "--name", config.ApplicationName);

            // Create .gitignore file
            _cmd.Exec("dotnet", "new", "gitignore");
            var filename = Path.Combine(ProjectRoot, ".gitignore");
            var gitignore = File.ReadAllLines(filename).ToList();
            gitignore.Insert(0, "# HalApplicationBuilder");
            gitignore.Insert(1, $"/{HALAPP_DLL_COPY_TARGET}/*");
            File.WriteAllLines(filename, gitignore);

            return this;
        }
        /// <summary>
        /// Program.cs ファイルを編集し、必要なソースコードを追記します。
        /// </summary>
        /// <returns></returns>
        internal HalappProject EditProgramCs() {
            _log?.WriteLine($"Program.cs ファイルを書き換えます。");
            var programCsPath = Path.Combine(ProjectRoot, "Program.cs");
            var lines = File.ReadAllLines(programCsPath).ToList();
            var regex1 = new Regex(@"^.*[a-zA-Z]+ builder = .+;$");
            var position1 = lines.FindIndex(regex1.IsMatch);
            if (position1 == -1) throw new InvalidOperationException("Program.cs の中にIServiceCollectionを持つオブジェクトを初期化する行が見つかりません。");
            lines.InsertRange(position1 + 1, new[] {
                    $"",
                    $"/* HalApplicationBuilder によって自動生成されたコード ここから */",
                    $"var runtimeRootDir = System.IO.Directory.GetCurrentDirectory();",
                    $"HalApplicationBuilder.Runtime.HalAppDefaultConfigurer.Configure(builder.Services, runtimeRootDir);",
                    $"// HTMLのエンコーディングをUTF-8にする(日本語のHTMLエンコード防止)",
                    $"builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options => {{",
                    $"    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All);",
                    $"}});",
                    $"// npm start で実行されるポートがASP.NETのそれと別なので",
                    $"builder.Services.AddCors(options => {{",
                    $"    options.AddDefaultPolicy(builder => {{",
                    $"        builder.AllowAnyOrigin()",
                    $"            .AllowAnyMethod()",
                    $"            .AllowAnyHeader();",
                    $"    }});",
                    $"}});",
                    $"/* HalApplicationBuilder によって自動生成されたコード ここまで */",
                    $"",
                });

            var regex2 = new Regex(@"^.*[a-zA-Z]+ app = .+;$");
            var position2 = lines.FindIndex(regex2.IsMatch);
            if (position2 == -1) throw new InvalidOperationException("Program.cs の中にappオブジェクトを初期化する行が見つかりません。");
            lines.InsertRange(position2 + 1, new[] {
                    $"",
                    $"/* HalApplicationBuilder によって自動生成されたコード ここから */",
                    $"// 前述AddCorsの設定をするならこちらも必要",
                    $"app.UseCors();",
                    $"/* HalApplicationBuilder によって自動生成されたコード ここまで */",
                    $"",
                });
            File.WriteAllLines(programCsPath, lines);

            return this;
        }
        /// <summary>
        /// コードの自動生成を行います。
        /// </summary>
        /// <param name="log">ログ出力先</param>
        public virtual HalappProject UpdateAutoGeneratedCode() {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 必要なNuGetパッケージを参照に加えます。
        /// </summary>
        internal HalappProject AddNugetPackages() {
            _log?.WriteLine($"Microsoft.EntityFrameworkCore パッケージへの参照を追加します。");
            _cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore");

            _log?.WriteLine($"Microsoft.EntityFrameworkCore.Proxies パッケージへの参照を追加します。");
            _cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Proxies");

            _log?.WriteLine($"Microsoft.EntityFrameworkCore.Design パッケージへの参照を追加します。"); // migration add に必要
            _cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Design");

            _log?.WriteLine($"Microsoft.EntityFrameworkCore.Sqlite パッケージへの参照を追加します。");
            _cmd.Exec("dotnet", "add", "package", "Microsoft.EntityFrameworkCore.Sqlite");

            return this;
        }
        /// <summary>
        /// halapp.dllとその依存先をプロジェクトディレクトリにコピーする。実行時にRuntimeContextを参照しているため
        /// </summary>
        internal virtual HalappProject AddReferenceToHalappDll() {
            _log?.WriteLine($"halapp.dll を参照に追加します。");

            // dllのコピー
            var halappDirCopySource = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var halappDirCopyDist = Path.Combine(ProjectRoot, HALAPP_DLL_COPY_TARGET);
            DotnetEx.IO.CopyDirectory(halappDirCopySource, halappDirCopyDist, deleteOnlyDist: true);

            // csprojファイルを編集: csprojファイルを開く
            const string HALAPP_INCLUDE = "halapp";
            var config = ReadConfig();
            var csprojPath = Path.Combine(ProjectRoot, $"{config.ApplicationName}.csproj");
            var csproj = Microsoft.Build.Construction.ProjectRootElement.Open(csprojPath);

            // csprojファイルを編集: 既に設定があるなら削除
            var itemGroup = csproj.ItemGroups.SingleOrDefault(group => group.Items.Any(item => item.Include == HALAPP_INCLUDE));
            if (itemGroup != null) csproj.RemoveChild(itemGroup);

            // csprojファイルを編集: halapp.dll への参照を追加する（dll参照は dotnet add でサポートされていないため）
            itemGroup = csproj.AddItemGroup();
            var reference = itemGroup.AddItem("Reference", include: HALAPP_INCLUDE);
            reference.AddMetadata("HintPath", Path.Combine(HALAPP_DLL_COPY_TARGET, "halapp.dll"));

            // csprojファイルを編集: ビルド時に halapp.dll が含まれるディレクトリがコピーされるようにする
            var none = itemGroup.AddItem("None", Path.Combine(HALAPP_DLL_COPY_TARGET, "**", "*.*"));
            none.AddMetadata("CopyToOutputDirectory", "Always");

            csproj.Save();

            return this;
        }
        /// <summary>
        /// halapp.xml が無い場合作成します。
        /// </summary>
        internal HalappProject EnsureCreateHalappXml(string applicationName) {
            var xmlPath = GetAggregateSchemaPath();

            if (!File.Exists(xmlPath)) {
                var rootNamespace = applicationName.ToCSharpSafe();
                var config = new Config {
                    ApplicationName = applicationName,
                    DbContextName = "MyDbContext",
                    DbContextNamespace = $"{rootNamespace}.EntityFramework",
                    EntityFrameworkDirectoryRelativePath = "EntityFramework/__AutoGenerated",
                    EntityNamespace = $"{rootNamespace}.EntityFramework.Entities",
                    MvcControllerDirectoryRelativePath = "Controllers/__AutoGenerated",
                    MvcControllerNamespace = $"{rootNamespace}.Controllers",
                    MvcModelDirectoryRelativePath = "Models/__AutoGenerated",
                    MvcModelNamespace = $"{rootNamespace}.Models",
                    MvcViewDirectoryRelativePath = "Views/_AutoGenerated",
                    OutProjectDir = ".",
                };
                var xmlContent = new XDocument(config.ToXmlWithRoot());
                using var sw = new StreamWriter(xmlPath, append: false, encoding: new UTF8Encoding(false));
                sw.WriteLine(xmlContent.ToString());
            }

            return this;
        }
        /// <summary>
        /// データベースが存在しない場合に新規作成します。
        /// </summary>
        /// <returns></returns>
        public HalappProject EnsureCreateDatabase() {

            // sqliteファイル出力先フォルダが無い場合は作成する
            var dbDir = Path.Combine(ProjectRoot, "bin", "Debug");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            var migrator = new DotnetEf(new Cmd {
                WorkingDirectory = ProjectRoot,
                Verbose = _verbose,
                CancellationToken = _cancellationToken,
            });
            if (!migrator.GetMigrations().Any()) {
                migrator.AddMigration();
            }

            return this;
        }

        /// <summary>
        /// 必要なnpmモジュールをインストールします。
        /// </summary>
        public HalappProject InstallDependencies() {
            var npmProcess = new DotnetEx.Cmd {
                WorkingDirectory = Path.Combine(ProjectRoot, REACT_DIR),
                CancellationToken = _cmd.CancellationToken,
                Verbose = _verbose,
            };
            npmProcess.Exec("npm", "ci");

            // dotnetはビルド時に自動的にインストールされるので何もしない

            return this;
        }

        /// <summary>
        /// プロジェクトをビルドします。
        /// </summary>
        public void Build() {
            UpdateAutoGeneratedCode();
            _cmd.Exec("dotnet", "build");

            // TODO npm build
        }
        /// <summary>
        /// デバッグを開始します。
        /// </summary>
        public void StartDebugging() {

            if (!IsValidDirectory()) return;
            if (_cancellationToken == null) throw new InvalidOperationException($"CancellationToken is required when debug.");

            var config = ReadConfig();
            var migrator = new DotnetEf(new Cmd {
                WorkingDirectory = ProjectRoot,
                Verbose = _verbose,
                CancellationToken = _cancellationToken,
            });

            // 以下の2種類のキャンセルがあるので統合する
            // - ユーザーの操作による halapp debug 全体のキャンセル
            // - 集約定義ファイル更新によるビルドのキャンセル
            CancellationTokenSource? rebuildCancellation = null;
            CancellationTokenSource? linkedTokenSource = null;

            // バックグラウンド処理の宣言
            DotnetEx.Cmd.Background? dotnetRun = null;
            DotnetEx.Cmd.Background? npmStart = null;

            // ファイル変更監視用オブジェクト
            FileSystemWatcher? watcher = null;

            try {
                InstallDependencies();

                var changed = false;

                watcher = new FileSystemWatcher(ProjectRoot);
                watcher.Filter = HALAPP_XML_NAME;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += (_, _) => {
                    changed = true;
                    rebuildCancellation?.Cancel();
                };

                npmStart = new DotnetEx.Cmd.Background {
                    WorkingDirectory = Path.Combine(ProjectRoot, REACT_DIR),
                    Filename = "npm",
                    Args = new[] { "start" },
                    CancellationToken = _cancellationToken,
                    Verbose = _verbose,
                };

                // 監視開始
                watcher.EnableRaisingEvents = true;
                npmStart.Restart();

                // リビルドの度に実行される処理
                while (true) {
                    dotnetRun?.Dispose();
                    rebuildCancellation?.Dispose();
                    linkedTokenSource?.Dispose();

                    rebuildCancellation = new CancellationTokenSource();
                    linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        _cancellationToken!.Value,
                        rebuildCancellation.Token);

                    try {
                        // ソースファイル再生成 & npm watch による自動更新
                        UpdateAutoGeneratedCode();

                        // DB定義の更新。
                        // halapp debug を実行するたびにマイグレーションファイルが積み重なっていくのを防ぐため、
                        // 最新のリリース済みマイグレーションまで巻き戻す
                        var latestRelease = string.Empty; // TODO: halapp release コマンドの結果と突き合わせる
                        var latestReleaseMigration = string.IsNullOrWhiteSpace(latestRelease)
                            ? migrator.GetMigrations().First().Name
                            : latestRelease;
                        migrator.RemoveMigrationsUntil(latestReleaseMigration);
                        migrator.AddMigration();

                        dotnetRun = new DotnetEx.Cmd.Background {
                            WorkingDirectory = ProjectRoot,
                            Filename = "dotnet",
                            Args = new[] { "run", "--launch-profile", "https" },
                            CancellationToken = linkedTokenSource.Token,
                            Verbose = _verbose,
                        };
                        dotnetRun.Restart();

                    } catch (OperationCanceledException) when (_cancellationToken!.Value.IsCancellationRequested) {
                        throw; // デバッグ自体を中断

                    } catch (OperationCanceledException) when (rebuildCancellation.IsCancellationRequested) {
                        continue; // 実行中のビルドを中断してもう一度最初から

                    } catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }

                    changed = false;

                    // 次の更新まで待機
                    while (changed == false) {
                        Thread.Sleep(100);
                        _cancellationToken!.Value.ThrowIfCancellationRequested();
                    }
                }

            } catch (OperationCanceledException) {
                Console.WriteLine("デバッグを中断します。");

            } finally {
                rebuildCancellation?.Dispose();
                linkedTokenSource?.Dispose();
                dotnetRun?.Dispose();
                npmStart?.Dispose();
                watcher?.Dispose();
            }
        }

        internal Core.AppSchema Inspect() {
            var xmlFullPath = GetAggregateSchemaPath();
            using var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            var config = Core.Config.GetDefault(xDocument.Root!.Name.LocalName);

            if (!Core.AppSchemaBuilder.FromXml(xDocument, out var builder, out var errors)) {
                throw new InvalidOperationException(errors.Join(Environment.NewLine));
            }
            if (!builder.TryBuild(out var appSchema, out var errors1)) {
                throw new InvalidOperationException(errors1.Join(Environment.NewLine));
            }

            return appSchema;
        }

        /// <summary>
        /// dotnet ef のラッパー
        /// </summary>
        private class DotnetEf {

            internal DotnetEf(Cmd cmd) {
                _cmd = cmd;
            }
            private readonly Cmd _cmd;

            internal IEnumerable<Migration> GetMigrations() {
                var output = _cmd
                    .ReadOutput(
                        "dotnet", "ef", "migrations", "list",
                        "--prefix-output", // ビルド状況やの行頭には "info:" が、マイグレーション名の行頭には "data:" がつくので、その識別のため
                        "--configuration", "Release"); // このクラスの処理が走っているとき、基本的には dotnet run も並走しているので、Releaseビルドを指定しないとビルド先が競合して失敗してしまう

                var regex = new Regex(@"^data:\s*([^\s]+)(\s\(Pending\))?$", RegexOptions.Multiline);
                return output
                    .Select(line => regex.Match(line))
                    .Where(match => match.Success)
                    .Select(match => new Migration {
                        Name = match.Groups[1].Value,
                        Pending = match.Groups.Count == 3,
                    });
            }
            internal void RemoveMigrationsUntil(string migrationName) {
                // そのマイグレーションが適用済みだと migrations remove できないので、まず database update する
                _cmd.Exec("dotnet", "ef", "database", "update", migrationName, "--configuration", "Release");

                // リリース済みマイグレーションより後のマイグレーションを消す
                while (GetMigrations().Last().Name != migrationName) {
                    _cmd.Exec("dotnet", "ef", "migrations", "remove", "--configuration", "Release");
                }
            }
            internal void AddMigration() {
                var migrationCount = GetMigrations().Count();
                var nextMigrationId = migrationCount.ToString("000000000000");

                _cmd.Exec("dotnet", "ef", "migrations", "add", nextMigrationId, "--configuration", "Release");
                _cmd.Exec("dotnet", "ef", "database", "update", "--configuration", "Release");
            }

            internal struct Migration {
                internal string Name { get; set; }
                internal bool Pending { get; set; }
            }
        }
    }



    internal class HalappProject2 : HalappProject {

        internal HalappProject2(string projetctRoot, CancellationToken? cancellationToken, TextWriter? log, bool verbose) : base(projetctRoot, cancellationToken, log, verbose) {

        }

        internal override HalappProject AddReferenceToHalappDll() {
            // 2ではhalapp.dllへの依存を排除しているので何もしない
            return this;
        }

        public override HalappProject UpdateAutoGeneratedCode() {
            if (!IsValidDirectory()) return this;

            _log?.WriteLine($"コード自動生成開始");

            var xmlFullPath = GetAggregateSchemaPath();
            using var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            var config = Core.Config.GetDefault(xDocument.Root!.Name.LocalName);

            if (!Core.AppSchemaBuilder.FromXml(xDocument, out var builder, out var errors)) {
                throw new InvalidOperationException(errors.Join(Environment.NewLine));
            }
            if (!builder.TryBuild(out var appSchema, out var errors1)) {
                throw new InvalidOperationException(errors1.Join(Environment.NewLine));
            }

            var ctx = new CodeRenderingContext {
                Config = config,
                Schema = appSchema,
            };
            // TODO: Reactのソースの自動生成ができたらコメントアウトを解除して復活させる
            //var reactProjectTemplate = Path.Combine(
            //    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            //    "CodeRendering",
            //    "ReactAndWebApi",
            //    "project-template");

            DirectorySetupper.Directory(ProjectRoot, _log, dir => {

                dir.Directory("__AutoGenerated", genDir => {
                    genDir.Generate(new Configure(ctx));

                    foreach (var aggregate in ctx.Schema.RootAggregates()) {
                        genDir.Generate(new AggFile(aggregate, ctx));
                    }

                    genDir.Directory("Util", utilDir => {
                        utilDir.Generate(new CodeRendering.Util.RuntimeSettings(ctx));
                        utilDir.Generate(new CodeRendering.Util.DotnetExtensions(ctx.Config));
                        utilDir.Generate(new CodeRendering.Util.FromTo(ctx.Config));
                        utilDir.Generate(new CodeRendering.Util.InstanceKey(ctx));
                        utilDir.Generate(new CodeRendering.Util.AggregateInstanceKeyNamePair(ctx.Config));
                        utilDir.DeleteOtherFiles();
                    });
                    genDir.Directory("Web", controllerDir => {
                        controllerDir.Generate(new CodeRendering.Presentation.AggregateInstanceBase(ctx));
                        controllerDir.Generate(new CodeRendering.Presentation.SearchConditionBase(ctx));
                        controllerDir.Generate(new CodeRendering.Presentation.SearchResultBase(ctx));
                        controllerDir.Generate(new DebuggerController(ctx));
                        controllerDir.DeleteOtherFiles();
                    });
                    genDir.Directory("EntityFramework", efDir => {
                        efDir.Generate(new DbContext(ctx));
                        efDir.DeleteOtherFiles();
                    });
                    genDir.DeleteOtherFiles();
                });

                // TODO: Reactのソースの自動生成ができたらコメントアウトを解除して復活させる
                //if (!Directory.Exists(Path.Combine(dir.Path, REACT_DIR))) {
                //    DotnetEx.IO.CopyDirectory(reactProjectTemplate, Path.Combine(dir.Path, REACT_DIR));
                //}

                dir.Directory(Path.Combine(REACT_DIR, "src", "__AutoGenerated"), reactDir => {

                    // TODO: Reactのソースの自動生成ができたらコメントアウトを解除して復活させる
                    //reactDir.Generate(Path.Combine(reactProjectTemplate, "src", "__AutoGenerated", "halapp.css"));
                    //reactDir.Generate(Path.Combine(reactProjectTemplate, "src", "__AutoGenerated", "halapp.types.ts"));
                    reactDir.Generate(new CodeRendering.ReactAndWebApi.index(ctx, "pages"));
                    reactDir.Generate(new CodeRendering.ReactAndWebApi.menuItems(ctx, "pages"));

                    // TODO: Reactのソースの自動生成ができたらコメントアウトを解除して復活させる
                    //reactDir.Directory("components", componentsDir => {
                    //    var source = Path.Combine(reactProjectTemplate, "src", "__AutoGenerated", "components");
                    //    foreach (var file in Directory.GetFiles(source)) componentsDir.Generate(file);
                    //    foreach (var template in ComboBox.All(ctx)) componentsDir.Generate(template);
                    //    componentsDir.DeleteOtherFiles();
                    //});
                    // TODO: Reactのソースの自動生成ができたらコメントアウトを解除して復活させる
                    //reactDir.Directory("hooks", componentsDir => {
                    //    var source = Path.Combine(reactProjectTemplate, "src", "__AutoGenerated", "hooks");
                    //    foreach (var file in Directory.GetFiles(source)) componentsDir.Generate(file);
                    //    componentsDir.DeleteOtherFiles();
                    //});
                    reactDir.Directory("pages", pageDir => {
                        foreach (var template in ReactComponent.All(ctx)) pageDir.Generate(template);
                        pageDir.DeleteOtherFiles();
                    });
                    reactDir.DeleteOtherFiles();
                });
            });

            return this;
        }

        private class DirectorySetupper {
            internal static void Directory(string absolutePath, TextWriter? log, Action<DirectorySetupper> fn) {
                var setupper = new DirectorySetupper(absolutePath, log);
                setupper.Directory("", fn);
            }
            private DirectorySetupper(string path, TextWriter? log) {
                Path = path;
                _log = log;
                _generated = new HashSet<string>();
            }

            internal string Path { get; }
            private readonly TextWriter? _log;
            private readonly HashSet<string> _generated;
            internal void Directory(string relativePath, Action<DirectorySetupper> fn) {
                var fullpath = System.IO.Path.Combine(Path, relativePath);
                if (!System.IO.Directory.Exists(fullpath))
                    System.IO.Directory.CreateDirectory(fullpath);

                _generated.Add(fullpath);

                fn(new DirectorySetupper(System.IO.Path.Combine(Path, relativePath), _log));
            }

            internal void Generate(ITemplate template) {
                var file = System.IO.Path.Combine(Path, template.FileName);

                _generated.Add(file);

                _log?.WriteLine($"CREATING ... {file}");
                using var sw = new StreamWriter(file, append: false, encoding: GetEncoding(file));
                sw.WriteLine(template.TransformText());
            }
            internal void Generate(string copySourceFile) {
                var copyTargetFile = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(copySourceFile));

                _generated.Add(copyTargetFile);

                _log?.WriteLine($"CREATING ... {copyTargetFile}");
                var encoding = GetEncoding(copySourceFile);
                using var reader = new StreamReader(copySourceFile, encoding);
                using var writer = new StreamWriter(copyTargetFile, append: false, encoding: encoding);
                while (!reader.EndOfStream) {
                    writer.WriteLine(reader.ReadLine());
                }
            }
            internal void DeleteOtherFiles() {
                var deleteFiles = System.IO.Directory
                    .GetFiles(Path)
                    .Where(path => !_generated.Contains(path));
                foreach (var file in deleteFiles) {
                    if (!File.Exists(file)) continue;
                    _log?.WriteLine($"DELETE ... {file}");
                    File.Delete(file);
                }
                var deletedDirectories = System.IO.Directory
                    .GetDirectories(Path)
                    .Where(path => !_generated.Contains(path));
                foreach (var dir in deletedDirectories) {
                    if (!System.IO.Directory.Exists(dir)) continue;
                    _log?.WriteLine($"DELETE ... {dir}");
                    System.IO.Directory.Delete(dir);
                }
            }

            private static Encoding GetEncoding(string filepath) {
                return System.IO.Path.GetExtension(filepath).ToLower() == "cs"
                    ? Encoding.UTF8 // With BOM
                    : new UTF8Encoding(false);
            }
        }
    }
}
