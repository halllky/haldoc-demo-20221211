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
            _generatedProject = app;
            CoreLibrary = new CoreLibrary.DirectoryEditor(this, app.CoreLibrary);
            WebApiProject = new WebApiProject.DirectoryEditor(this, app.WebApiProject);
            ReactProject = new ReactProject.DirectoryEditor(this, app.ReactProject);
            CliProject = new CliProject.DirectoryEditor(this, app.CliProject);

            Config = app.ReadConfig();
            Schema = app.BuildSchema();
            Options = options;
        }

        private readonly GeneratedProject _generatedProject;

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

        internal void OnEndContext() {

            CoreLibrary.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.RenderConfigureServices());
                genDir.Generate(EnumDefs.Render(this));

                // アプリケーションサービス
                Features.Storing.Customize.RenderBaseClasses(this);
                genDir.Generate(new ApplicationService().RenderToCoreLibrary());

                // 集約ごとのファイル
                foreach (var aggFile in CoreLibrary._itemsByAggregate.Values) {
                    genDir.Generate(aggFile.RenderCoreLibrary());
                }

                // Entity Framework Core のソースを生成
                genDir.Directory("EntityFramework", efDir => {
                    efDir.Generate(new DbContextClass(Config).RenderDeclaring());
                });

                // ユニットテスト用コード
                if (Options.OverwriteConcreteAppSrvFile) {
                    genDir.Directory("..", outOfGenDir => {
                        outOfGenDir.Generate(new ApplicationService().RenderConcreteClass());
                    });
                }
            });

            CoreLibrary.UtilDir(utilDir => {
                utilDir.Generate(RuntimeSettings.Render(this));
                utilDir.Generate(Parts.Utility.DotnetExtensions.RenderCoreLibrary());
                utilDir.Generate(Parts.Utility.FromTo.Render(this));
                utilDir.Generate(Parts.Utility.UtilityClass.RenderJsonConversionMethods(this));
            });

            WebApiProject.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.RenderWebapiConfigure());

                // 集約ごとのファイル
                foreach (var aggFile in CoreLibrary._itemsByAggregate.Values) {
                    genDir.Generate(aggFile.RenderWebApi());
                }
            });

            WebApiProject.UtilDir(utilDir => {
                utilDir.Generate(Parts.Utility.DotnetExtensions.RenderToWebApiProject());
            });

            ReactProject.AutoGeneratedDir(reactDir => {

                reactDir.CopyEmbeddedResource(EmbeddedResources
                    .Get("react", "src", "__autoGenerated", "index.tsx"));
                reactDir.CopyEmbeddedResource(EmbeddedResources
                    .Get("react", "src", "__autoGenerated", "nijo-default-style.css"));

                reactDir.Generate(TypesTsx.Render(this, CoreLibrary._itemsByAggregate.Select(x => KeyValuePair.Create(x.Key, x.Value.TypeScriptDataTypes))));
                reactDir.Generate(MenuTsx.Render(this));

                reactDir.Directory("collection", layoutDir => {
                    var resources = EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "collection");
                    foreach (var resource in resources) {
                        layoutDir.CopyEmbeddedResource(resource);
                    }
                });
                reactDir.Directory("input", userInputDir => {
                    var resources = EmbeddedResources
                        .Enumerate("react", "src", "__autoGenerated", "input");
                    foreach (var resource in resources) {
                        userInputDir.CopyEmbeddedResource(resource);
                    }

                    // TODO: どの集約がコンボボックスを作るのかをModelsが決められるようにしたい
                    userInputDir.Generate(Features.Storing.ComboBox.RenderDeclaringFile(this));
                });
            });

            ReactProject.UtilDir(reactUtilDir => {
                var resources = EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "util");
                foreach (var resource in resources) {
                    reactUtilDir.CopyEmbeddedResource(resource);
                }

                // TODO: Modelsが決められるようにしたい
                reactUtilDir.Generate(NavigationWrapper.Render());
            });

            ReactProject.PagesDir(pageDir => {

                pageDir.Generate(DashBoard.Generate(this));

                var resources = EmbeddedResources
                    .Enumerate("react", "src", "__autoGenerated", "pages");
                foreach (var resource in resources) {
                    pageDir.CopyEmbeddedResource(resource);
                }

                foreach (var group in ReactProject.ReactPages.GroupBy(p => p.DirNameInPageDir)) {
                    pageDir.Directory(group.Key, aggregatePageDir => {
                        foreach (var page in group) {
                            aggregatePageDir.Generate(page.GetSourceFile());
                        }
                    });
                }
            });

            CliProject.AutoGeneratedDir(genDir => {
                genDir.Generate(Configure.RenderCliConfigure());
            });

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
            var allFiles = Directory.GetFiles(_generatedProject.CoreLibrary.AutoGeneratedDir, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_generatedProject.WebApiProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(_generatedProject.ReactProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(_generatedProject.CliProject.AutoGeneratedDir, "*", SearchOption.AllDirectories));
            foreach (var file in allFiles) {
                if (IsHandled(file)) continue;
                if (!File.Exists(file)) continue;
                File.Delete(file);
            }
            var allDirectories = Directory.GetDirectories(_generatedProject.CoreLibrary.AutoGeneratedDir, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(_generatedProject.WebApiProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetDirectories(_generatedProject.ReactProject.AutoGeneratedDir, "*", SearchOption.AllDirectories))
                .Concat(Directory.GetDirectories(_generatedProject.CliProject.AutoGeneratedDir, "*", SearchOption.AllDirectories));
            foreach (var dir in allDirectories) {
                if (IsHandled(dir)) continue;
                if (!Directory.Exists(dir)) continue;
                Directory.Delete(dir, true);
            }
        }
        #endregion 生成されなかったファイルの削除
    }
}
