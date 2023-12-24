using Nijo.Architecture;
using Nijo.Architecture.WebServer;
using Nijo.Architecture.WebClient;
using Nijo.Core;
using Nijo.Util.DotnetEx;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using Nijo.Util.CodeGenerating;
using Nijo.Features;

namespace Nijo {
    public sealed class NijoCodeGenerator {
        internal NijoCodeGenerator(GeneratedProject project, ILogger? log) {
            _project = project;
            _log = log;
        }

        private readonly GeneratedProject _project;
        private readonly ILogger? _log;

        /// <summary>
        /// 新規プロジェクトのひな形を作成します。
        /// </summary>
        internal NijoCodeGenerator CreateProjectTemplate(string applicationName) {
            _log?.LogInformation($"プロジェクトを作成します。");

            // プロジェクトディレクトリの作成
            if (Directory.Exists(_project.ProjectRoot) || File.Exists(_project.ProjectRoot)) {
                throw new InvalidOperationException($"Directory is already exists: {_project.ProjectRoot}");
            }
            Directory.CreateDirectory(_project.ProjectRoot);

            // nijo.xmlの作成
            var xmlPath = _project.SchemaXml.GetPath();
            var config = new Config {
                ApplicationName = applicationName,
                DbContextName = "MyDbContext",
            };
            var xmlContent = new XDocument(config.ToXmlWithRoot());
            using var sw = new StreamWriter(xmlPath, append: false, encoding: new UTF8Encoding(false));
            sw.WriteLine(xmlContent.ToString());

            // プロジェクトルートディレクトリのいろいろ作成
            var nijoExeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var gitignoreFrom = Path.Combine(nijoExeDir, "ApplicationTemplates", ".gitignore");
            var gitignoreTo = Path.Combine(_project.ProjectRoot, ".gitignore");
            File.Copy(gitignoreFrom, gitignoreTo);

            // reactディレクトリのコピー
            // webapiディレクトリのコピー
            var reactProjectTemplate = Path.Combine(nijoExeDir, "ApplicationTemplates", "REACT_AND_WEBAPI", "react");
            var webapiProjectTemplate = Path.Combine(nijoExeDir, "ApplicationTemplates", "REACT_AND_WEBAPI", "webapi");
            Util.DotnetEx.IO.CopyDirectory(reactProjectTemplate, _project.WebClientProjectRoot);
            Util.DotnetEx.IO.CopyDirectory(webapiProjectTemplate, _project.WebApiProjectRoot);

            // reactのデバッグ用コードを除去
            var debugRoom = Path.Combine(_project.WebClientProjectRoot, "src", "debug-room");
            Directory.Delete(debugRoom, true);

            var appTsx = Path.Combine(_project.WebClientProjectRoot, "src", "App.tsx");
            File.WriteAllText(appTsx, $$"""
                import { ApplicationRoot } from './__autoGenerated/application'

                function App() {

                  return (
                    <ApplicationRoot />
                  )
                }

                export default App
                """, new UTF8Encoding(false));

            // ソースコード中の "REACT_AND_WEBAPI" という文字をプロジェクト名に置換する
            var programCs = Path.Combine(_project.WebApiProjectRoot, "Program.cs");
            var beforeReplace = File.ReadAllText(programCs);
            var afterReplace = beforeReplace.Replace("REACT_AND_WEBAPI", config.RootNamespace);
            File.WriteAllText(programCs, afterReplace);

            var beforeCsproj = Path.Combine(_project.WebApiProjectRoot, "REACT_AND_WEBAPI.csproj");
            var afterCsproj = Path.Combine(_project.WebApiProjectRoot, $"{config.ApplicationName}.csproj");
            File.Move(beforeCsproj, afterCsproj);

            // 自動生成されないクラスの初期値
            var appSrv = new ApplicationService();
            var overrideAppSrv = Path.Combine(_project.WebApiProjectRoot, appSrv.ConcreteClassFileName);
            File.WriteAllText(overrideAppSrv, $$"""
                namespace {{config.RootNamespace}} {
                    /// <summary>
                    /// 自動生成された検索機能や登録機能を上書きする場合はこのクラス内でそのメソッドやプロパティをoverrideしてください。
                    /// </summary>
                    public partial class {{appSrv.ConcreteClass}} : {{appSrv.ClassName}} {
                        public {{appSrv.ConcreteClass}}(IServiceProvider serviceProvider) : base(serviceProvider) { }


                    }
                }
                """);

            return this;
        }
        /// <summary>
        /// コードの自動生成を行います。
        /// </summary>
        /// <param name="log">ログ出力先</param>
        public NijoCodeGenerator UpdateAutoGeneratedCode() {

            _log?.LogInformation($"コード自動生成開始: {_project.ProjectRoot}");

            var ctx = new CodeRenderingContext {
                Config = _project.ReadConfig(),
                Schema = _project.BuildSchema(),
            };
            ctx.WebapiDir = DirectorySetupper.StartSetup(ctx, Path.Combine(_project.WebApiProjectRoot, "__AutoGenerated"));
            ctx.ReactDir = DirectorySetupper.StartSetup(ctx, Path.Combine(_project.WebClientProjectRoot, "src", "__autoGenerated"));

            var features = GetFeatures().ToArray();
            var nonAggregateFeatures = features
                .OfType<NijoFeatureBaseNonAggregate>();
            foreach (var feature in nonAggregateFeatures) {
                feature.GenerateCode(ctx);
            }

            var aggregateFeatures = features
                .OfType<NijoFeatureBaseByAggregate>()
                .ToArray();
            var defaultFeature = aggregateFeatures.Single(f => f is MasterDataFeature);
            var handlers = Handlers
                .GetAll()
                .ToDictionary(kv => kv.Key, kv => aggregateFeatures.Single(f => f.GetType() == kv.Value));
            foreach (var rootAggregate in ctx.Schema.RootAggregates()) {
                if (!string.IsNullOrWhiteSpace(rootAggregate.Item.Options.Handler)
                    && handlers.TryGetValue(rootAggregate.Item.Options.Handler, out var feature)) {
                    feature.GenerateCode(ctx, rootAggregate);
                } else {
                    // 特に指定の無い集約はMasterData扱い
                    defaultFeature.GenerateCode(ctx, rootAggregate);
                }
            }

            // 絶対作成する機能を登録する
            ctx.Render<Infrastucture>(_ => { });

            // 複数の集約から1個のソースが作成されるものはこのタイミングで作成
            ctx.OnEndContext();

            _log?.LogInformation($"コード自動生成終了: {_project.ProjectRoot}");
            return this;
        }

        private IEnumerable<NijoFeatureBase> GetFeatures() {
            var featureAssemblies = new[] { Assembly.GetExecutingAssembly() };

            var featureTypes = featureAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(NijoFeatureBase)));

            foreach (var type in featureTypes) {
                NijoFeatureBase instance;
                try {
                    instance = (NijoFeatureBase)Activator.CreateInstance(type)!;
                } catch (Exception ex) {
                    throw new InvalidOperationException($"{nameof(NijoFeatureBase)} クラスには引数なしのコンストラクタが必要です。", ex);
                }
                yield return instance;
            }
        }
        internal static class Handlers {
            internal static KeyValuePair<string, Type> MasterData => KeyValuePair.Create("master-data", typeof(MasterDataFeature));
            internal static KeyValuePair<string, Type> View => KeyValuePair.Create("view", typeof(AggregateSearchFeature));
            internal static KeyValuePair<string, Type> Command => KeyValuePair.Create("command", typeof(CommandFeature));

            internal static IEnumerable<KeyValuePair<string, Type>> GetAll() {
                yield return MasterData;
                yield return View;
                yield return Command;
            }
        }

        /// <summary>
        /// ソースコード自動生成処理を直感的に書けるようにするためのクラス
        /// </summary>
        public class DirectorySetupper {
            internal static DirectorySetupper StartSetup(CodeRenderingContext ctx, string absolutePath) {
                return new DirectorySetupper(ctx, absolutePath);
            }
            internal static void StartSetup(CodeRenderingContext ctx, string absolutePath, Action<DirectorySetupper> fn) {
                var setupper = StartSetup(ctx, absolutePath);
                setupper.Directory("", fn);
            }
            private DirectorySetupper(CodeRenderingContext ctx, string path) {
                Path = path;
                _ctx = ctx;
            }

            internal string Path { get; }

            private readonly CodeRenderingContext _ctx;

            public void Directory(string relativePath, Action<DirectorySetupper> fn) {
                var fullpath = System.IO.Path.Combine(Path, relativePath);
                if (!System.IO.Directory.Exists(fullpath))
                    System.IO.Directory.CreateDirectory(fullpath);
                _ctx.Handle(fullpath);

                fn(new DirectorySetupper(_ctx, System.IO.Path.Combine(Path, relativePath)));
            }

            public void Generate(SourceFile sourceFile) {
                var file = System.IO.Path.Combine(Path, sourceFile.FileName);
                _ctx.Handle(file);

                using var sw = new StreamWriter(file, append: false, encoding: GetEncoding(file));
                var ext = System.IO.Path.GetExtension(file).ToLower();
                sw.NewLine = ext == ".cs" ? "\r\n" : "\n";
                var content = sourceFile
                    .RenderContent()
                    .Replace(Environment.NewLine, sw.NewLine);
                sw.WriteLine(content);
            }

            public void CopyFrom(string copySourceFile) {
                var copyTargetFile = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(copySourceFile));
                _ctx.Handle(copyTargetFile);

                var encoding = GetEncoding(copySourceFile);
                using var reader = new StreamReader(copySourceFile, encoding);
                using var writer = new StreamWriter(copyTargetFile, append: false, encoding: encoding);
                while (!reader.EndOfStream) {
                    writer.WriteLine(reader.ReadLine());
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
