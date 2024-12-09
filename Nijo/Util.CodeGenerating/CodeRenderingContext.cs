using Nijo.Core;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Parts.WebServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Nijo.Util.CodeGenerating {
    public sealed class CodeRenderingContext {
        internal CodeRenderingContext(GeneratedProject app, NijoCodeGenerator.CodeGenerateOptions options) {
            GeneratedProject = app;
            CoreLibrary = new CoreLibrary.DirectoryEditor(this, app.CoreLibrary);
            WebApiProject = new WebApiProject.DirectoryEditor(this, app.WebApiProject);
            ReactProject = new ReactProject.DirectoryEditor(this, app.ReactProject);
            CliProject = new CliProject.DirectoryEditor(this, app.CliProject);

            Config = app.ReadConfig();
            Schema = app.BuildSchema();
            Options = options;
        }

        public GeneratedProject GeneratedProject { get; }

        public Config Config { get; }
        public AppSchema Schema { get; }
        public NijoCodeGenerator.CodeGenerateOptions Options { get; }

        /// <summary>自動生成されるC#クラスライブラリプロジェクト</summary>
        public CoreLibrary.DirectoryEditor CoreLibrary { get; }
        /// <summary>自動生成される ASP.NET Core API プロジェクト</summary>
        public WebApiProject.DirectoryEditor WebApiProject { get; }
        /// <summary>自動生成される React.js プロジェクト</summary>
        public ReactProject.DirectoryEditor ReactProject { get; }
        /// <summary>自動生成されるコンソールアプリケーションのプロジェクト</summary>
        public CliProject.DirectoryEditor CliProject { get; }

        /// <summary>ソースコードレンダリング中に少なくとも一度以上登場したモデル</summary>
        private readonly Dictionary<Type, Models.IModel> _handledModels = new();
        internal Models.IModel GetModel<T>() where T : Models.IModel, new() {
            if (!_handledModels.TryGetValue(typeof(T), out var model)) {
                model = new T();
                _handledModels.Add(typeof(T), model);
            }
            return model;
        }
        internal Models.IModel GetModel(string key) {
            // 該当のキーにヒットするモデルを検索
            var model = NijoCodeGenerator.Models
                .GetAll()
                .Single(kv => kv.Key == key)
                .Value
                .Invoke();
            var modelType = model.GetType();

            // 未登録の場合は登録する
            if (!_handledModels.TryGetValue(modelType, out var registered)) {
                _handledModels.Add(modelType, model);
            }
            return model;
        }

        /// <summary>
        /// 複数のソース生成処理が1つのファイルにまとめて書き込むような形のファイル。
        /// コード生成処理の途中でファイルの途中に挿入されるソースコードの文字列が五月雨で登録され、
        /// コード生成処理の最後で実ファイルにレンダリングされる。
        /// </summary>
        private readonly HashSet<ISummarizedFile> _summarizedFiles = new();
        /// <summary>
        /// コード生成処理の最後で <see cref="ISummarizedFile"/> のレンダリングが行われるよう予約します。
        /// </summary>
        /// <typeparam name="T">ファイルの型</typeparam>
        internal T UseSummarizedFile<T>() where T : ISummarizedFile, new() {
            var found = _summarizedFiles.SingleOrDefault(item => item.GetType() == typeof(T));
            if (found != null) return (T)found;

            var newInstance = new T();
            _summarizedFiles.Add(newInstance);
            return newInstance;
        }
        /// <summary>
        /// コード生成処理の最後で <see cref="ISummarizedFile"/> のレンダリングが行われるよう予約します。
        /// </summary>
        /// <param name="summarizedFile">複数のソース生成処理が1つのファイルにまとめて書き込むような形のファイル</param>
        internal void UseSummarizedFile(ISummarizedFile summarizedFile) {
            _summarizedFiles.Add(summarizedFile);
        }

        /// <summary>
        /// コード自動生成の最後に実行される処理。
        /// アプリケーション全体に影響するようなソースコードなどを生成する。
        /// </summary>
        internal void OnEndContext() {

            UseSummarizedFile<Configure>();

            CoreLibrary.UtilDir(utilDir => {
                var resources = EmbeddedResources
                    .Enumerate("core", "__AutoGenerated", "Util");
                foreach (var resource in resources) {
                    utilDir.CopyEmbeddedResource(resource, line => {
                        return line
                            .Replace("NIJO_APPLICATION_TEMPLATE_WebApi", Config.RootNamespace)
                            .Replace("NIJO_APPLICATION_TEMPLATE", Config.RootNamespace);
                    });
                }

                utilDir.Generate(RuntimeSettings.Render(this));
                utilDir.Generate(DotnetExtensions.RenderCoreLibrary());
                utilDir.Generate(Parts.BothOfClientAndServer.FromTo.Render(this));
            });

            WebApiProject.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.RenderWebapiConfigure());
            });

            WebApiProject.UtilDir(utilDir => {
                var resources = EmbeddedResources
                    .Enumerate("webapi", "__AutoGenerated", "Util");
                foreach (var resource in resources) {
                    utilDir.CopyEmbeddedResource(resource, line => {
                        return line
                            .Replace("NIJO_APPLICATION_TEMPLATE_WebApi", Config.RootNamespace)
                            .Replace("NIJO_APPLICATION_TEMPLATE", Config.RootNamespace);
                    });
                }

                utilDir.Generate(DotnetExtensions.RenderToWebApiProject());
            });

            ReactProject.AutoGeneratedDir(reactDir => {

                reactDir.CopyEmbeddedResource(EmbeddedResources.Get("react", "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyEmbeddedResource(EmbeddedResources.Get("react", "src", "__autoGenerated", "autogenerated-components.tsx"));
                reactDir.CopyEmbeddedResource(EmbeddedResources.Get("react", "src", "__autoGenerated", "autogenerated-hooks.tsx"));

                reactDir.Directory(RefToFile.DIR_NAME, refToDir => {
                    refToDir.CopyEmbeddedResource(EmbeddedResources.Get("react", "src", "__autoGenerated", "ref_to", "index.ts"));
                });

                UseSummarizedFile<UiContext>();

                reactDir.Generate(NijoDefaultStyleCss.Generate(EmbeddedResources));

                reactDir.Directory("collection", layoutDir => {
                    var resources = EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "collection");
                    foreach (var resource in resources) {
                        layoutDir.CopyEmbeddedResource(resource);
                    }
                });
                reactDir.Directory(Parts.ReactProject.INPUT, userInputDir => {
                    var resources = EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "input");
                    foreach (var resource in resources) {
                        userInputDir.CopyEmbeddedResource(resource);
                    }
                });
            });

            ReactProject.UtilDir(reactUtilDir => {
                var resources = EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "util");
                foreach (var resource in resources) {
                    reactUtilDir.CopyEmbeddedResource(resource);
                }
            });

            ReactProject.PagesDir(pageDir => {

                pageDir.Generate(DashBoard.Generate(this));

                var resources = EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "pages");
                foreach (var resource in resources) {
                    pageDir.CopyEmbeddedResource(resource);
                }
            });

            CliProject.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.RenderCliConfigure());

                var resources = EmbeddedResources
                    .Enumerate("cli", "__AutoGenerated");
                foreach (var resource in resources) {
                    genDir.CopyEmbeddedResource(resource, line => {
                        return line.Replace("NIJO_APPLICATION_TEMPLATE_Cli", Config.RootNamespace);
                    });
                }
            });


            // メンバー型のコード生成。列挙体の定義などの生成を行う。
            var cellType = UseSummarizedFile<Parts.WebClient.DataTable.CellType>();
            var allMemberTypes = Schema
                .AllAggregates()
                .SelectMany(agg => agg.GetMembers())
                .OfType<AggregateMember.ValueMember>()
                .Select(vm => vm.Options.MemberType)
                .Distinct();
            foreach (var memberType in allMemberTypes) {
                memberType.GenerateCode(this);
                cellType.Add(memberType.RenderDataTableColumnDefHelper(this));
            }

            // モデルと関係するがルート集約1個と対応しないソースコードを生成する
            foreach (var model in _handledModels.Values) {
                model.GenerateCode(this);
            }

            foreach (var summarizedFile in _summarizedFiles.OrderBy(x => x.RenderingOrder)) {
                summarizedFile.OnEndGenerating(this);
            }

            CleanUnhandledFilesAndDirectories();
        }

        public Assembly ExecutingAssembly => _executingAssembly ??= Assembly.GetExecutingAssembly();
        private Assembly? _executingAssembly;

        internal EmbeddedResource.Collection EmbeddedResources => _embeddedResources ??= new EmbeddedResource.Collection(ExecutingAssembly);
        private EmbeddedResource.Collection? _embeddedResources;

        #region 生成されなかったファイルの削除
        private readonly HashSet<string> _handled = new();
        internal void Handle(string fullpath) => _handled.Add(Path.GetFullPath(fullpath));
        internal bool IsHandled(string fullpath) => _handled.Contains(Path.GetFullPath(fullpath));
        private void CleanUnhandledFilesAndDirectories() {
            var allFiles = Directory.GetFiles(GeneratedProject.CoreLibrary.AutoGeneratedDir, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(GeneratedProject.WebApiProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(GeneratedProject.ReactProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(GeneratedProject.CliProject.AutoGeneratedDir, "*", SearchOption.AllDirectories));
            foreach (var file in allFiles) {
                if (IsHandled(file)) continue;
                if (!File.Exists(file)) continue;
                File.Delete(file);
            }
            var allDirectories = Directory.GetDirectories(GeneratedProject.CoreLibrary.AutoGeneratedDir, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(GeneratedProject.WebApiProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetDirectories(GeneratedProject.ReactProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetDirectories(GeneratedProject.CliProject.AutoGeneratedDir, "*", SearchOption.AllDirectories));
            foreach (var dir in allDirectories) {
                if (IsHandled(dir)) continue;
                if (!Directory.Exists(dir)) continue;
                Directory.Delete(dir, true);
            }
        }
        #endregion 生成されなかったファイルの削除
    }
}
