using HalApplicationBuilder.CodeRendering.ReactAndWebApi;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
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

namespace HalApplicationBuilder {
    public sealed class GeneratedProject {

        private const string HALAPP_XML_NAME = "halapp.xml";
        private const string REACT_DIR = "ClientApp";
        internal const string REACT_PAGE_DIR = "pages";
        private const string HALAPP_DLL_COPY_TARGET = "halapp-resource";

        /// <summary>
        /// 新しいhalappプロジェクトを作成します。
        /// </summary>
        /// <param name="applicationName">アプリケーション名</param>
        /// <param name="verbose">ログの詳細出力を行うかどうか</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static GeneratedProject Create(string? applicationName, bool verbose, bool keepTempIferror, CancellationToken cancellationToken, TextWriter? log = null) {

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new InvalidOperationException($"Please specify name of new application. example 'halapp create my-new-app'");

            if (Path.GetInvalidFileNameChars().Any(applicationName.Contains))
                throw new InvalidOperationException($"'{applicationName}' contains invalid characters for a file name.");

            var projectRoot = Path.Combine(Directory.GetCurrentDirectory(), applicationName);
            if (Directory.Exists(projectRoot))
                throw new InvalidOperationException($"'{projectRoot}' is already exists.");

            var ramdomName = $"halapp.temp.{Path.GetRandomFileName()}";
            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), ramdomName);

            var error = false;
            try {
                var tempProject = new GeneratedProject(tempDir);
                var setupManager = tempProject.StartSetup(verbose, cancellationToken, log);

                Directory.CreateDirectory(tempDir);

                setupManager.EnsureCreateHalappXml(applicationName);

                setupManager.DotnetNew();

                setupManager.EditProgramCs();
                setupManager.UpdateAutoGeneratedCode();

                setupManager.AddNugetPackages();
                setupManager.AddReferenceToHalappDll();

                setupManager.EnsureCreateRuntimeSettingFile();
                setupManager.EnsureCreateDatabase();

                setupManager.InstallNodeModules();

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
                if (Directory.Exists(projectRoot)) throw new InvalidOperationException($"プロジェクトディレクトリを {projectRoot} に移動できません。");
                Directory.Move(tempDir, projectRoot);

                log?.WriteLine("プロジェクト作成完了");

                return new GeneratedProject(projectRoot);

            } catch {
                error = true;
                throw;

            } finally {
                if (Directory.Exists(tempDir)
                    && (keepTempIferror == false || error == false)) {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        /// <summary>
        /// 既存のhalappプロジェクトを開きます。
        /// </summary>
        /// <param name="path">プロジェクトルートディレクトリの絶対パス</param>
        /// <returns>作成されたプロジェクトを表すオブジェクト</returns>
        public static GeneratedProject Open(string? path) {
            if (string.IsNullOrWhiteSpace(path))
                return new GeneratedProject(Directory.GetCurrentDirectory());
            else if (Path.IsPathRooted(path))
                return new GeneratedProject(path);
            else
                return new GeneratedProject(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private GeneratedProject(string projetctRoot) {
            if (string.IsNullOrWhiteSpace(projetctRoot))
                throw new ArgumentException($"'{nameof(projetctRoot)}' is required.");

            ProjectRoot = projetctRoot;
        }

        private string ProjectRoot { get; }
        private AppSchema ReadSchema() {
            var xmlFullPath = Path.Combine(ProjectRoot, HALAPP_XML_NAME);
            using var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            var appSchema = AppSchema.FromXml(xDocument);
            return appSchema;
        }
        private Config ReadConfig() {
            var xmlFullPath = Path.Combine(ProjectRoot, HALAPP_XML_NAME);
            using var stream = DotnetEx.IO.OpenFileWithRetry(xmlFullPath);
            using var reader = new StreamReader(stream);
            var xmlContent = reader.ReadToEnd();
            var xDocument = XDocument.Parse(xmlContent);
            var config = Core.Config.FromXml(xDocument);
            return config;
        }

        /// <summary>
        /// このディレクトリがhalappのものとして妥当なものかどうかを検査します。
        /// </summary>
        /// <param name="log">エラー内容出力</param>
        /// <returns></returns>
        public bool IsValidDirectory(TextWriter? log = null) {
            var errors = new List<string>();

            if (Path.GetInvalidPathChars().Any(ProjectRoot.Contains))
                errors.Add($"Invalid path format: '{ProjectRoot}'");

            if (!Directory.Exists(ProjectRoot))
                errors.Add($"Directory '{ProjectRoot}' is not exist.");

            var halappXml = Path.Combine(ProjectRoot, HALAPP_XML_NAME);
            if (!File.Exists(halappXml))
                errors.Add($"'{halappXml}' is not found.");

            if (log != null) {
                foreach (var error in errors) log.WriteLine(error);
            }
            return errors.Count == 0;
        }

        #region CODE GENERATING
        public SetupManager StartSetup(bool verbose, CancellationToken cancellationToken, TextWriter? log = null) {
            return new SetupManager(this, verbose, cancellationToken, log);
        }
        public sealed class SetupManager {
            internal SetupManager(GeneratedProject project, bool verbose, CancellationToken cancellationToken, TextWriter? log = null) {
                // 個別のメソッドに都度引数を渡すのが面倒なのでクラスにまとめた
                _project = project;
                _verbose = verbose;
                _cancellationToken = cancellationToken;
                _log = log;

                _cmd = new DotnetEx.Cmd {
                    WorkingDirectory = _project.ProjectRoot,
                    CancellationToken = _cancellationToken,
                    Verbose = _verbose,
                };
            }
            private readonly GeneratedProject _project;
            private readonly bool _verbose;
            private readonly CancellationToken _cancellationToken;
            private readonly TextWriter? _log;

            private readonly Cmd _cmd;

            /// <summary>
            /// dotnet new コマンドを実行します。
            /// </summary>
            public SetupManager DotnetNew() {
                _log?.WriteLine($"プロジェクトを作成します。");

                var config = _project.ReadConfig();
                _cmd.Exec("dotnet", "new", "webapi", "--output", ".", "--name", config.ApplicationName);

                // Create .gitignore file
                _cmd.Exec("dotnet", "new", "gitignore");
                var filename = Path.Combine(_project.ProjectRoot, ".gitignore");
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
            public SetupManager EditProgramCs() {
                _log?.WriteLine($"Program.cs ファイルを書き換えます。");
                var programCsPath = Path.Combine(_project.ProjectRoot, "Program.cs");
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
            public SetupManager UpdateAutoGeneratedCode() {

                if (!_project.IsValidDirectory(_log)) return this;

                _log?.WriteLine($"コード自動生成開始");

                var config = _project.ReadConfig();
                var rootAggregates = _project.ReadSchema().GetRootAggregates(config).ToArray();
                var allAggregates = rootAggregates
                    .SelectMany(a => a.GetDescendantsAndSelf())
                    .ToArray();

                _log?.WriteLine("コード自動生成: DI設定");
                using (var sw = new StreamWriter(Path.Combine(_project.ProjectRoot, "HalappDefaultConfigure.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.DefaultRuntimeConfigTemplate(config).TransformText());
                }

                _log?.WriteLine("コード自動生成: スキーマ定義");
                using (var sw = new StreamWriter(Path.Combine(_project.ProjectRoot, "halapp.json"), append: false, encoding: Encoding.UTF8)) {
                    var schema = new Serialized.AppSchemaJson {
                        Config = config.ToJson(onlyRuntimeConfig: true),
                        Aggregates = rootAggregates.Select(a => a.ToJson()).ToArray(),
                    };
                    sw.Write(System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), // 日本語用
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, // nullのフィールドをシリアライズしない
                    }));
                }

                var modelDir = Path.Combine(_project.ProjectRoot, config.MvcModelDirectoryRelativePath);
                if (Directory.Exists(modelDir)) Directory.Delete(modelDir, recursive: true);
                Directory.CreateDirectory(modelDir);

                _log?.WriteLine("コード自動生成: UI Model");
                using (var sw = new StreamWriter(Path.Combine(modelDir, "Models.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.UIModelsTemplate(config, allAggregates).TransformText());
                }

                var efSourceDir = Path.Combine(_project.ProjectRoot, config.EntityFrameworkDirectoryRelativePath);
                if (Directory.Exists(efSourceDir)) Directory.Delete(efSourceDir, recursive: true);
                Directory.CreateDirectory(efSourceDir);

                _log?.WriteLine("コード自動生成: DbContext");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbContext.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.DbContextTemplate(config).TransformText());
                }

                _log?.WriteLine("コード自動生成: Entity定義");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Entities.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.EntityClassTemplate(config, allAggregates).TransformText());
                }
                _log?.WriteLine("コード自動生成: DbSet");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "DbSet.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.DbSetTemplate(config, allAggregates).TransformText());
                }
                _log?.WriteLine("コード自動生成: OnModelCreating");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "OnModelCreating.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.OnModelCreatingTemplate(config, allAggregates).TransformText());
                }
                _log?.WriteLine("コード自動生成: Search");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "Search.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.SearchMethodTemplate(config, rootAggregates).TransformText());
                }
                _log?.WriteLine("コード自動生成: AutoCompleteSource");
                using (var sw = new StreamWriter(Path.Combine(efSourceDir, "AutoCompleteSource.cs"), append: false, encoding: Encoding.UTF8)) {
                    sw.Write(new CodeRendering.EFCore.AutoCompleteSourceTemplate(config, allAggregates).TransformText());
                }

                // Web API
                _log?.WriteLine("コード自動生成: .NET Core Web API Controller");
                using (var sw = new StreamWriter(Path.Combine(_project.ProjectRoot, "Controllers", "__AutoGenerated.cs"), append: false, encoding: Encoding.UTF8)) {
                    var template = new WebApiControllerTemplate(config, rootAggregates);
                    sw.Write(template.TransformText());
                }
                using (var sw = new StreamWriter(Path.Combine(_project.ProjectRoot, "Controllers", "Debugger.cs"), append: false, encoding: Encoding.UTF8)) {
                    var template = new WebApiDebuggerTemplate(config);
                    sw.Write(template.TransformText());
                }

                // React.js
                var tsProjectSource = Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location)!,
                    "CodeRendering",
                    "ReactAndWebApi",
                    "project-template");
                var tsProjectDist = Path.Combine(
                    _project.ProjectRoot,
                    REACT_DIR);
                var tsAutoGeneratedSource = Path.Combine(tsProjectSource, "src", "__AutoGenerated");
                var tsAutoGeneratedDist = Path.Combine(tsProjectDist, "src", "__AutoGenerated");

                if (!Directory.Exists(tsProjectDist)) {
                    DotnetEx.IO.CopyDirectory(tsProjectSource, tsProjectDist);
                }

                // 集約定義
                var utf8withoutBOM = new UTF8Encoding(false);
                _log?.WriteLine("コード自動生成: 集約のTypeScript型定義");
                using (var sw = new StreamWriter(Path.Combine(tsAutoGeneratedDist, ReactTypeDefTemplate.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new ReactTypeDefTemplate();
                    sw.Write(template.TransformText());
                }
                // コンポーネント
                _log?.WriteLine("コード自動生成: 集約のReactコンポーネント");
                var reactPageDir = Path.Combine(tsAutoGeneratedDist, REACT_PAGE_DIR);
                if (!Directory.Exists(reactPageDir)) Directory.CreateDirectory(reactPageDir);
                var updatetdReactFiles = new HashSet<string>();
                foreach (var rootAggregate in rootAggregates) {
                    var template = new ReactComponentTemplate(rootAggregate);
                    var filepath = Path.Combine(reactPageDir, template.FileName);
                    using var sw = new StreamWriter(filepath, append: false, encoding: utf8withoutBOM);
                    sw.Write(template.TransformText());

                    updatetdReactFiles.Add(filepath);
                }
                var deleteFiles = Directory
                    .GetFiles(reactPageDir)
                    .Where(file => !updatetdReactFiles.Contains(file));
                foreach (var filepath in deleteFiles) {
                    File.Delete(filepath);
                }

                // menuItems.tsx
                using (var sw = new StreamWriter(Path.Combine(tsAutoGeneratedDist, menuItems.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new menuItems(rootAggregates);
                    sw.Write(template.TransformText());
                }
                // index.ts
                using (var sw = new StreamWriter(Path.Combine(tsAutoGeneratedDist, index.FILE_NAME), append: false, encoding: utf8withoutBOM)) {
                    var template = new index(rootAggregates);
                    sw.Write(template.TransformText());
                }

                // 集約定義に基づかないreactモジュール
                _log?.WriteLine("コード自動生成: halapp標準モジュール");

                // 集約定義に基づかないreactモジュール: components
                var componentsIn = Path.Combine(tsAutoGeneratedSource, "components");
                var componentsOut = Path.Combine(tsAutoGeneratedDist, "components");
                DotnetEx.IO.CopyDirectory(componentsIn, componentsOut, deleteOnlyDist: true);

                // 集約定義に基づかないreactモジュール: hooks
                var hooksIn = Path.Combine(tsAutoGeneratedSource, "hooks");
                var hooksOut = Path.Combine(tsAutoGeneratedDist, "hooks");
                DotnetEx.IO.CopyDirectory(hooksIn, hooksOut, deleteOnlyDist: true);

                // 集約定義に基づかないreactモジュール: halapp.css
                using (var sr = new StreamReader(Path.Combine(tsAutoGeneratedSource, "halapp.css")))
                using (var sw = new StreamWriter(Path.Combine(tsAutoGeneratedDist, "halapp.css"), append: false, encoding: utf8withoutBOM)) {
                    sw.WriteLine(sr.ReadToEnd());
                }

                _log?.WriteLine("コード自動生成終了");

                return this;
            }
            /// <summary>
            /// 必要なNuGetパッケージを参照に加えます。
            /// </summary>
            public SetupManager AddNugetPackages() {
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
            public SetupManager AddReferenceToHalappDll() {
                _log?.WriteLine($"halapp.dll を参照に追加します。");

                // dllのコピー
                var halappDirCopySource = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var halappDirCopyDist = Path.Combine(_project.ProjectRoot, HALAPP_DLL_COPY_TARGET);
                DotnetEx.IO.CopyDirectory(halappDirCopySource, halappDirCopyDist, deleteOnlyDist: true);

                // csprojファイルを編集: csprojファイルを開く
                const string HALAPP_INCLUDE = "halapp";
                var config = _project.ReadConfig();
                var csprojPath = Path.Combine(_project.ProjectRoot, $"{config.ApplicationName}.csproj");
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
            public SetupManager EnsureCreateHalappXml(string applicationName) {
                var xmlPath = Path.Combine(_project.ProjectRoot, HALAPP_XML_NAME);

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
            /// 実行時設定ファイルを規定値で作成します。
            /// </summary>
            public SetupManager EnsureCreateRuntimeSettingFile() {
                var runtimeSettingPath = Path.Combine(_project.ProjectRoot, CodeRendering.DefaultRuntimeConfigTemplate.HALAPP_RUNTIME_SERVER_SETTING_JSON);
                if (!File.Exists(runtimeSettingPath)) {
                    _log?.WriteLine($"{CodeRendering.DefaultRuntimeConfigTemplate.HALAPP_RUNTIME_SERVER_SETTING_JSON} ファイルを作成します。");
                    using var sw = new StreamWriter(runtimeSettingPath, false, new UTF8Encoding(false));
                    sw.WriteLine(Runtime.RuntimeSettings.Server.GetDefault().ToJson());
                }
                return this;
            }
            /// <summary>
            /// データベースが存在しない場合に新規作成します。
            /// </summary>
            /// <returns></returns>
            public SetupManager EnsureCreateDatabase() {
                EnsureCreateRuntimeSettingFile();

                // sqliteファイル出力先フォルダが無い場合は作成する
                var dbDir = Path.Combine(_project.ProjectRoot, "bin", "Debug");
                if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

                var migrator = new DotnetEf(new Cmd {
                    WorkingDirectory = _project.ProjectRoot,
                    Verbose = _verbose,
                    CancellationToken = _cancellationToken,
                });
                if (!migrator.GetMigrations().Any()) {
                    migrator.AddMigration();
                }

                return this;
            }
            /// <summary>
            /// 必要なnpmモジュールを node_moduels にインストールします。
            /// </summary>
            public SetupManager InstallNodeModules() {
                var npmProcess = new DotnetEx.Cmd {
                    WorkingDirectory = Path.Combine(_project.ProjectRoot, REACT_DIR),
                    CancellationToken = _cmd.CancellationToken,
                    Verbose = _verbose,
                };
                npmProcess.Exec("npm", "ci");

                return this;
            }
        }
        #endregion CODE GENERATING

        #region DEBUG COMMAND
        /// <summary>
        /// デバッグを開始します。
        /// </summary>
        public void StartDebugging(bool verbose, CancellationToken cancellationToken, TextWriter? log = null) {

            if (!IsValidDirectory(log)) return;

            var config = ReadConfig();
            var setupManager = StartSetup(verbose, cancellationToken, log);
            var migrator = new DotnetEf(new Cmd {
                WorkingDirectory = ProjectRoot,
                Verbose = verbose,
                CancellationToken = cancellationToken,
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
                    CancellationToken = cancellationToken,
                    Verbose = verbose,
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
                        cancellationToken,
                        rebuildCancellation.Token);

                    try {
                        // ソースファイル再生成 & npm watch による自動更新
                        setupManager.UpdateAutoGeneratedCode();

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
                            Verbose = verbose,
                        };
                        dotnetRun.Restart();

                    } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
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
                        cancellationToken.ThrowIfCancellationRequested();
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
        #endregion DEBUG COMMAND
    }
}
