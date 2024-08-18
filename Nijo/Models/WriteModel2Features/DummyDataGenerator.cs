using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Models.RefTo;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// デバッグ用ダミーデータ作成関数
    /// </summary>
    internal class DummyDataGenerator : ISummarizedFile {
        private const int DATA_COUNT = 4;

        private readonly List<GraphNode<Aggregate>> _aggregates = new();

        internal void Add(GraphNode<Aggregate> aggregate) {
            _aggregates.Add(aggregate);
        }

        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {
            context.ReactProject.UtilDir(utilDir => {
                utilDir.Generate(Render());
            });
        }

        private SourceFile Render() => new SourceFile {
            FileName = "useDummyDataGenerator2.ts",
            RenderContent = ctx => {
                var random = new Random(0);
                var ordered = _aggregates
                    .OrderByDataFlow();
                var refSearchHooks = _aggregates
                    .SelectMany(agg => agg.EnumerateThisAndDescendants())
                    .Select(agg => new RefSearchHookTemp(agg));

                return $$"""
                    import { useCallback } from 'react'
                    import * as AggregateType from '../autogenerated-types'
                    import * as AggregateHook from '../autogenerated-hooks'
                    import { useHttpRequest } from './Http'

                    export const useDummyDataGenerator2 = () => {
                      const { get } = useHttpRequest()
                      const { batchUpdateImmediately } = AggregateHook.{{BatchUpdateWriteModel.HOOK_NAME}}()
                    {{refSearchHooks.SelectTextTemplate(x => $$"""
                      const { {{RefSearchMethod.LOAD}}: {{x.Load}} } = AggregateHook.{{x.Hook}}()
                    """)}}

                      return useCallback(async () => {

                    {{ordered.SelectTextTemplate(x => $$"""
                        {{WithIndent(RenderAggregate(x, random), "    ")}}

                    """)}}
                        return true
                      }, [
                        get,
                        batchUpdateImmediately,
                    {{refSearchHooks.SelectTextTemplate(x => $$"""
                        {{x.Load}},
                    """)}}
                      ])
                    }
                    """;
            },
        };

        private static string RenderAggregate(GraphNode<Aggregate> rootAggregate, Random random) {
            var descendants = rootAggregate.EnumerateDescendants();
            var instanceList = Enumerable
                .Range(0, DATA_COUNT)
                .Select(_ => $"data{random.Next(99999999):00000000}")
                .ToArray();
            var forSave = new DataClassForSave(rootAggregate, DataClassForSave.E_Type.Create);
            var response = $"response{random.Next(99999999):00000000}";

            return $$"""
                // ----------------------- {{rootAggregate.Item.DisplayName}}のダミーデータ作成 -----------------------
                {{instanceList.SelectTextTemplate(instance => $$"""
                const {{instance}} = AggregateType.{{forSave.TsNewObjectFnName}}()
                """)}}

                {{rootAggregate.GetMembers().Where(m => m.DeclaringAggregate == rootAggregate).SelectTextTemplate(member => $$"""
                {{instanceList.SelectTextTemplate((instance, index) => $$"""
                {{SetDummyValue(member, instance, index)}}
                """)}}
                """)}}

                {{descendants.SelectTextTemplate(agg => $$"""
                {{If(agg.IsChildrenMember(), () => $$"""
                {{instanceList.SelectTextTemplate(instance => $$"""
                {{instance}}.{{ObjectPath(agg).Join(".")}} = {{NewObject(agg)}}
                """)}}
                """)}}
                {{agg.GetMembers().Where(m => m.DeclaringAggregate == agg).SelectTextTemplate(member => $$"""
                {{instanceList.SelectTextTemplate((instance, index) => $$"""
                {{SetDummyValue(member, instance, index)}}
                """)}}
                """)}}

                """)}}
                const {{response}} = await batchUpdateImmediately([{{instanceList.Join(", ")}}].map(data => ({
                  {{DataClassForSaveBase.DATA_TYPE_TS}}: '{{DataClassForSaveBase.GetEnumValueOf(rootAggregate)}}',
                  {{DataClassForSaveBase.ADD_MOD_DEL_TS}}: 'ADD',
                  {{DataClassForSaveBase.VALUES_TS}}: data,
                })))
                if (!{{response}}) return false

                """;

            static IEnumerable<string> ObjectPath(GraphNode<Aggregate> agg) {
                return agg
                    .PathFromEntry()
                    .Select(path => path.Terminal.As<Aggregate>().IsChildrenMember()
                                 && path.Terminal.As<Aggregate>() != agg
                        ? $"{path.RelationName}[0]"
                        : $"{path.RelationName}");
            }

            static string NewObject(GraphNode<Aggregate> agg) {
                var forSave = new DataClassForSave(agg, DataClassForSave.E_Type.Create);
                return agg.IsChildrenMember()
                    ? $"[AggregateType.{forSave.TsNewObjectFnName}()]"
                    : $"AggregateType.{forSave.TsNewObjectFnName}()";
            }

            string SetDummyValue(AggregateMember.AggregateMemberBase member, string instance, int index) {
                var path = member.Owner
                    .PathFromEntry()
                    .Select(edge => edge.Terminal.As<Aggregate>().IsChildrenMember()
                        ? $"{edge.RelationName}[0]"
                        : edge.RelationName)
                    .Concat([member.MemberName]);

                if (member is AggregateMember.Variation variation) {
                    var key = random.Next(variation.VariationGroup.VariationAggregates.Count);
                    return $"{instance}.{path.Join(".")} = '{variation.GetGroupItems().ElementAt(key).TsValue}'";

                } else if (member is AggregateMember.Schalar schalar) {
                    static string RandomAlphabet(Random random, int length) {
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        return new string(chars[random.Next(chars.Length)], length);
                    }
                    static string RandomEnum(EnumList enumList, Random random) {
                        var randomItem = enumList
                            .Definition
                            .Items[random.Next(enumList.Definition.Items.Count)];
                        return $"'{randomItem.PhysicalName}'";
                    }

                    var dummyValue = schalar.Options.MemberType switch {
                        Core.AggregateMemberTypes.Boolean => "true",
                        EnumList enumList => RandomEnum(enumList, random),
                        Id => $"'{random.Next(99999999):00000000}'",
                        Integer => random.Next(999999).ToString(),
                        Numeric => $"{random.Next(999999)}.{random.Next(0, 99)}",
                        Sentence => "'XXXXXXXXXXXXXX\\nXXXXXXXXXXXXXX'",
                        Year => random.Next(1990, 2040).ToString(),
                        YearMonth => $"{random.Next(1990, 2040):0000}{random.Next(1, 12):00}",
                        YearMonthDay => $"'{new DateTime(2000, 1, 1).AddDays(random.Next(3000)):yyyy-MM-dd}'",
                        YearMonthDayTime => $"'{new DateTime(2000, 1, 1).AddDays(random.Next(3000)):yyyy-MM-dd}'",
                        Uuid => null, // 自動生成されるので
                        VariationSwitch => null, // Variationの分岐で処理済み
                        Word => $"'{RandomAlphabet(random, 10)}'",
                        _ => null, // 未定義
                    };
                    return dummyValue == null
                        ? SKIP_MARKER
                        : $"{instance}.{path.Join(".")} = {dummyValue}";

                } else if (member is AggregateMember.Ref @ref) {
                    var refSearch = new RefSearchHookTemp(@ref.RefTo);
                    var apiReturnType = new RefDisplayData(@ref.RefTo, @ref.RefTo);
                    var res = $"response{random.Next(99999999):00000000}";
                    return $$"""
                        const {{res}} = await {{refSearch.Load}}(AggregateType.{{refSearch.NewCondition}}())
                        {{instance}}.{{path.Join(".")}} = {{res}}[{{index}}]
                        """;

                } else if (member is AggregateMember.Child
                          || member is AggregateMember.Children
                          || member is AggregateMember.VariationItem) {
                    return SKIP_MARKER;

                } else {
                    throw new NotImplementedException();
                }
            }
        }

        private class RefSearchHookTemp {
            internal RefSearchHookTemp(GraphNode<Aggregate> aggregate) {
                _aggregate = aggregate;
                _refSearchMethod = new RefSearchMethod(aggregate, aggregate);
                _refSearchCondition = new RefSearchCondition(aggregate, aggregate);
            }
            private readonly GraphNode<Aggregate> _aggregate;
            private readonly RefSearchMethod _refSearchMethod;
            private readonly RefSearchCondition _refSearchCondition;

            internal string Hook => _refSearchMethod.ReactHookName;
            internal string Load => $"load{_aggregate.Item.PhysicalName}_{_aggregate.Item.UniqueId}";
            internal string NewCondition => _refSearchCondition.CreateNewObjectFnName;
        }
    }
}
