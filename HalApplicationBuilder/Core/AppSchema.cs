using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    internal class AppSchema {
        internal static AppSchema Empty() => new(string.Empty, DirectedGraph.Empty(), new HashSet<EnumDefinition>());

        internal AppSchema(string appName, DirectedGraph directedGraph, IReadOnlyCollection<EnumDefinition> enumDefinitions) {
            ApplicationName = appName;
            Graph = directedGraph;
            EnumDefinitions = enumDefinitions;
        }

        public string ApplicationName { get; }

        internal DirectedGraph Graph { get; }
        internal IEnumerable<GraphNode<Aggregate>> AllAggregates() {
            return Graph.Only<Aggregate>(); // TODO: これだと全集約エントリーになってしまうのでは
        }
        internal IEnumerable<GraphNode<Aggregate>> RootAggregates() {
            return AllAggregates().Where(aggregate => aggregate.IsRoot());
        }
        internal IEnumerable<GraphNode<DataView>> DataViews() {
            return Graph.Only<DataView>();
        }

        internal IReadOnlyCollection<EnumDefinition> EnumDefinitions { get; }

        /// <summary>
        /// デバッグ用TSV。Excelやスプレッドシートに貼り付けて構造の妥当性を確認するのに使う
        /// </summary>
        internal string DumpTsv() {
            var builder = new StringBuilder();

            var allAggregates = RootAggregates()
                .SelectMany(a => a.EnumerateThisAndDescendants())
                .ToArray();
            var maxIndent = allAggregates.Max(a => a.EnumerateAncestors().Count());

            var columns = new List<(string, Func<AggregateMember.AggregateMemberBase, string>)> {
                (
                    "メンバー型",
                    member => member.GetType().Name
                ), (
                    nameof(AggregateMember.ValueMember.Original),
                    member => member is AggregateMember.ValueMember vm
                        ? (vm.Original?.MemberName ?? "null")
                        : "-"
                ), (
                    nameof(AggregateMember.AggregateMemberBase.DeclaringAggregate),
                    member => member.DeclaringAggregate.ToString()
                ), (
                    nameof(AggregateMember.ValueMember.Declared),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Declared.MemberName
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.ForeignKeyOf),
                    member => member is AggregateMember.ValueMember vm
                        ? (vm.ForeignKeyOf?.MemberName ?? "null")
                        : "-"
                ), (
                    nameof(AggregateMember.RelationMember.Relation),
                    member => member is AggregateMember.RelationMember rel
                        ? rel.Relation.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.AggregateMemberBase.Order),
                    member => member.Order.ToString()
                ), (
                    nameof(AggregateMember.AggregateMemberBase.CSharpTypeName),
                    member => member.CSharpTypeName
                ), (
                    nameof(AggregateMember.AggregateMemberBase.TypeScriptTypename),
                    member => member.TypeScriptTypename
                ), (
                    nameof(AggregateMember.ValueMember.Options.MemberName),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.MemberName
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.MemberType),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.MemberType.GetType().Name
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKey),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKey.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsDisplayName),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsDisplayName.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKeyOfAncestor),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKeyOfAncestor.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.IsKeyOfRefTarget),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.IsKeyOfRefTarget.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.IsRequired),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.IsRequired.ToString()
                        : "-"
                ), (
                    nameof(AggregateMember.ValueMember.Options.InvisibleInGui),
                    member => member is AggregateMember.ValueMember vm
                        ? vm.Options.InvisibleInGui.ToString()
                        : "-"
                ),
            };

            builder.AppendLine($"# 集約");
            builder.AppendLine(string.Concat(Enumerable.Repeat("\t", maxIndent + 2)) + columns.Select(c => c.Item1).Join("\t"));

            foreach (var aggregate in allAggregates) {
                var depth = aggregate.EnumerateAncestors().Count();
                var indent1L = string.Concat(Enumerable.Repeat("\t", depth));
                var indent2L = "\t" + indent1L;
                var indent2R = string.Concat(Enumerable.Repeat("\t", maxIndent - depth + 1));
                builder.AppendLine($"{indent1L}{aggregate}");

                foreach (var member in aggregate.GetMembers()) {
                    builder.Append($"{indent2L}{member.MemberName}{indent2R}");
                    builder.AppendLine(columns.Select(c => c.Item2(member)).Join("\t"));
                }
            }

            return builder.ToString();
        }
    }
}
