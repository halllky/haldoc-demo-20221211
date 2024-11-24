using Nijo.Util.CodeGenerating;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest {
    /// <summary>
    /// テストデータパターン
    /// </summary>
    public abstract class DataPattern {

        /// <summary>
        /// テストパターン名
        /// </summary>
        protected abstract string PatternName { get; }

        /// <summary>
        /// OverridedApplicationService.cs の内容を書き換えます。
        /// </summary>
        protected virtual string OverridedAppSrvMethods() => string.Empty;
        /// <summary>
        /// App.tsx の内容を書き換えます。
        /// </summary>
        protected virtual string AppTsxCustomizer(CodeRenderingContext ctx) => $$"""
            import React from 'react'
            import useEvent from 'react-use-event-hook'
            import * as Icon from '@heroicons/react/24/outline'
            import { DefaultNijoApp } from './__autoGenerated'
            import * as Input from './__autoGenerated/input'
            import * as Hooks from './__autoGenerated/autogenerated-hooks'
            import * as Types from './__autoGenerated/autogenerated-types'
            import * as Comps from './__autoGenerated/autogenerated-components'
            
            function App() {
              return (
                <DefaultNijoApp
                  applicationName="{{ctx.Config.RootNamespace.Replace("\"", "&quot;")}}"
                  uiCustomizer={uiCustomizer}
                />
              )
            }
            
            // 自動生成されたソースコードに任意の処理やUIを追加編集する場合はここで設定してください。
            const uiCustomizer: Types.UiCustomizer = defaultUi => {
              return {
                ...defaultUi,
              }
            }
            
            export default App
            """;

        /// <summary>
        /// nijo.xmlの内容を返します。
        /// </summary>
        protected abstract string GetNijoXmlContents();

        /// <summary>
        /// 自動テストで作成されるプロジェクトのソースコードを所定のデータパターンのもので置き換えます。
        /// </summary>
        internal void UpdateAutoGeneratedCode(GeneratedProject project) {
            File.WriteAllText(project.SchemaXmlPath, GetNijoXmlContents());
            project.CodeGenerator.GenerateCode(new NijoCodeGenerator.CodeGenerateOptions {
                OnEndGenerating = ctx => {

                    // OverridedApplicationService.cs の内容を書き換える
                    ctx.CoreLibrary.AutoGeneratedDir(dir => {
                        File.WriteAllText(Path.Combine(dir.Path, "..", "OverridedApplicationService.cs"), $$"""
                            using Microsoft.EntityFrameworkCore;

                            namespace 自動テストで作成されたプロジェクト {
                                /// <summary>
                                /// 自動生成された検索機能や登録機能を上書きする場合はこのクラス内でそのメソッドやプロパティをoverrideしてください。
                                /// </summary>
                                public partial class OverridedApplicationService : AutoGeneratedApplicationService {
                                    public OverridedApplicationService(IServiceProvider serviceProvider) : base(serviceProvider) { }

                                    {{WithIndent(OverridedAppSrvMethods(), "        ")}}

                                    /// <summary>
                                    /// アプリケーションの設定を行うクラスです。
                                    /// 自動生成された初期設定のうちカスタマイズしたいものがある場合はここで該当の設定処理をオーバーライドしてください。
                                    /// </summary>
                                    public partial class CustomizedConfiguration : DefaultConfiguration {
                                    }
                                }
                            }
                            """, new UTF8Encoding(false, false));
                    });

                    // App.tsx の内容を書き換える
                    ctx.ReactProject.AutoGeneratedDir(dir => {
                        File.WriteAllText(
                            Path.Combine(dir.Path, "..", "App.tsx"),
                            AppTsxCustomizer(ctx).Replace("\r\n", "\n"),
                            new UTF8Encoding(false, false));
                    });
                },
            });
        }

        /// <summary>
        /// NUnitの <see cref="TestCaseSourceAttribute"/> がこのプロジェクト中にあるテストパターンの一覧を収集するのに必要なメソッド
        /// </summary>
        public static IEnumerable<object> Collect() {
            // テストプロジェクト中にある、このクラスを継承している型のインスタンスを集める
            return Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(DataPattern).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => (DataPattern)Activator.CreateInstance(type)!)
                .OrderByDescending(pattern => pattern.PatternName); // テストデータパターンの数字が大きい方がデータ構造が複雑でエラーが出やすい傾向にあるので大きい順から実行
        }

        public override string ToString() {
            return PatternName;
        }
    }


    /// <summary>
    /// テストデータパターン（XMLファイルにパターンを記載する類のもの）
    /// </summary>
    public abstract class DataPatternFromXml : DataPattern {
        protected DataPatternFromXml(string xmlFileName) {
            _xmlFileName = xmlFileName;
        }

        private readonly string _xmlFileName;

        protected sealed override string PatternName => Path.GetFileName(_xmlFileName);

        protected sealed override string GetNijoXmlContents() {
            var testProjectRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..");
            var xmlFullPath = Path.Combine(testProjectRoot, "DataPatterns", _xmlFileName);
            return File.ReadAllText(xmlFullPath).Trim();
        }
    }


    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class UseDataPatternsAttribute : TestCaseSourceAttribute {
        public UseDataPatternsAttribute() : base(typeof(DataPattern), nameof(DataPattern.Collect)) {

        }
    }
}
