using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    /// <summary>
    /// 参照先の表示や検索に関するフックやコンポーネントがレンダリングされるファイル。
    /// </summary>
    internal class RefToFile : ISummarizedFile {

        internal const string DIR_NAME = "ref-to";
        internal static string GetImportAlias(GraphNode<Aggregate> refTo) => $"RefTo{refTo.Item.PhysicalName.ToFileNameSafe()}";
        internal static string GetFileName(GraphNode<Aggregate> refTo) => $"RefTo{refTo.Item.PhysicalName.ToFileNameSafe()}.tsx";

        private readonly Dictionary<GraphNode<Aggregate>, List<string>> _sourceCode = new();
        internal void Add(GraphNode<Aggregate> refEntry, string sourceCode) {
            if (!_sourceCode.TryGetValue(refEntry, out var list)) {
                list = new List<string>();
                _sourceCode[refEntry] = list;
            }
            list.Add(sourceCode);
        }

        public void OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.AutoGeneratedDir(dir => {
                dir.Directory(DIR_NAME, refToDir => {
                    var indexTs = new List<string>();
                    foreach (var sourceFile in Render()) {
                        refToDir.Generate(sourceFile);
                        indexTs.Add($"export * from './{Path.GetFileNameWithoutExtension(sourceFile.FileName)}'");
                    }
                    refToDir.Generate(new() {
                        FileName = "index.ts",
                        RenderContent = _ => WithIndent(indexTs, ""),
                    });
                });
            });
        }

        private IEnumerable<SourceFile> Render() {
            foreach (var kv in _sourceCode) {
                yield return new SourceFile {
                    FileName = GetFileName(kv.Key),
                    RenderContent = ctx => {

                        // 参照先が別の参照先をもっているとき、このフォルダ内の別のファイルからimportする必要がある
                        var refToList = kv.Key.GetRoot()
                            .EnumerateThisAndDescendants()
                            .SelectMany(agg => agg.GetMembers())
                            .OfType<AggregateMember.Ref>()
                            .Select(@ref => new {
                                Alias = GetImportAlias(@ref.RefTo),
                                From = $"./{Path.GetFileNameWithoutExtension(GetFileName(@ref.RefTo))}",
                            })
                            .Distinct();

                        return $$"""
                            import React from 'react'
                            import useEvent from 'react-use-event-hook'
                            import * as ReactHookForm from 'react-hook-form'
                            import * as Icon from '@heroicons/react/24/outline'
                            import { Panel, PanelGroup, PanelResizeHandle, ImperativePanelHandle } from 'react-resizable-panels'
                            import * as Util from '../util'
                            import * as Input from '../input'
                            import * as Layout from '../collection'
                            import { VForm2 } from '../collection'
                            import * as Types from '../autogenerated-types'
                            import * as Hooks from '../autogenerated-hooks'
                            import { useCustomizerContext } from '../autogenerated-customizer'
                            {{refToList.SelectTextTemplate(x => $$"""
                            import * as {{x.Alias}} from '{{x.From}}'
                            """)}}

                            {{kv.Value.SelectTextTemplate(source => $$"""

                            {{source}}
                            """)}}
                            """;
                    },
                };
            }
        }
    }
}
