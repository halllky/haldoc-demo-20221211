using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.CodeRendering.Util;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Core
{
    internal class AggregateInstance : IGraphNode {
        internal AggregateInstance(Aggregate aggregate) : this(
            new NodeId($"INSTANCE::{aggregate.Id}"),
            aggregate.DisplayName.ToCSharpSafe()) {
        }
        internal AggregateInstance(NodeId id, string name) {
            Id = id;
            ClassName = $"{name}Instance";
            TypeScriptTypeName = name;
        }

        internal string ClassName { get; }
        internal string TypeScriptTypeName { get; }

        public NodeId Id { get; }

        public override string ToString() {
            return Id.Value;
        }

        internal const string BASE_CLASS_NAME = "AggregateInstanceBase";
        internal const string TO_DB_ENTITY_METHOD_NAME = "ToDbEntity";
        internal const string FROM_DB_ENTITY_METHOD_NAME = "FromDbEntity";

        internal class Property {
            internal required GraphNode<AggregateInstance> Owner { get; init; }
            internal required string CSharpTypeName { get; init; }
            internal required string TypeScriptTypename { get; init; }
            internal required string PropertyName { get; init; }
        }
        internal class SchalarProperty : Property {
            internal required IEFCoreEntity.IMember CorrespondingDbColumn { get; init; }
        }
        internal class ChildrenProperty : Property {
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class ChildProperty : Property {
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class VariationSwitchProperty : Property {
            internal required VariationGroup<AggregateInstance> Group { get; init; }
            internal required IEFCoreEntity.VariationGroupTypeIdentifier CorrespondingDbColumn { get; init; }

            internal required Config Config { get; init; }
            internal IEnumerable<VariationProperty> GetGroupItems() {
                foreach (var kv in Group.VariationAggregates) {
                    var dbEntity = kv.Value.Terminal.GetDbEntity();
                    var sameRelationWithEFCoreEntity = kv.Value.Terminal
                        .GetDbEntity()
                        .In
                        .Single(e => e.RelationName == kv.Value.RelationName)
                        .As<IEFCoreEntity>();
                    var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, Config);

                    yield return new VariationProperty {
                        Owner = Owner,
                        Group = this,
                        Key = kv.Key,
                        ChildAggregateInstance = kv.Value.Terminal,
                        CorrespondingNavigationProperty = navigationProperty,
                        CSharpTypeName = kv.Value.Terminal.Item.ClassName,
                        TypeScriptTypename = kv.Value.Terminal.Item.TypeScriptTypeName,
                        PropertyName = kv.Value.RelationName,
                    };
                }
            }
        }
        internal class VariationProperty : Property {
            internal required VariationSwitchProperty Group { get; init; }
            internal required string Key { get; init; }
            internal required GraphNode<AggregateInstance> ChildAggregateInstance { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
        }
        internal class RefProperty : Property {
            internal required GraphNode<AggregateInstance> RefTarget { get; init; }
            internal required NavigationProperty CorrespondingNavigationProperty { get; init; }
            internal required IEFCoreEntity.IMember[] CorrespondingDbColumns { get; init; }
        }
    }

    internal static class AggregateInstanceExtensions {
        internal static IEnumerable<AggregateInstance.Property> GetProperties(this GraphNode<AggregateInstance> node, Config config) {
            foreach (var prop in GetSchalarProperties(node)) yield return prop;
            foreach (var prop in GetChildrenProperties(node, config)) yield return prop;
            foreach (var prop in GetChildProperties(node, config)) yield return prop;
            foreach (var prop in GetVariationSwitchProperties(node, config)) yield return prop;
            foreach (var prop in GetVariationProperties(node, config)) yield return prop;
            foreach (var prop in GetRefProperties(node, config)) yield return prop;
        }

        internal static IEnumerable<AggregateInstance.SchalarProperty> GetSchalarProperties(this GraphNode<AggregateInstance> instance) {
            var dbEntityColumns = instance.GetDbEntity().GetColumns().ToArray();
            foreach (var member in instance.GetCorrespondingAggregate().GetSchalarMembers()) {
                yield return new AggregateInstance.SchalarProperty {
                    Owner = instance,
                    CorrespondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == member.Item.Name),
                    CSharpTypeName = member.Item.Type.GetCSharpTypeName(),
                    TypeScriptTypename = member.Item.Type.GetTypeScriptTypeName(),
                    PropertyName = member.Item.Name,
                };
            }
        }

        internal static IEnumerable<AggregateInstance.ChildrenProperty> GetChildrenProperties(this GraphNode<AggregateInstance> instance, Config config) {
            var initial = instance.GetDbEntity().Item.Id;
            foreach (var edge in instance.GetChildrenMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal
                    .GetDbEntity()
                    .In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, config);

                yield return new AggregateInstance.ChildrenProperty {
                    Owner = instance,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = $"List<{edge.Terminal.Item.ClassName}>",
                    TypeScriptTypename = $"{edge.Terminal.Item.TypeScriptTypeName}[]",
                    PropertyName = edge.RelationName,
                };
            }
        }
        internal static IEnumerable<AggregateInstance.ChildProperty> GetChildProperties(this GraphNode<AggregateInstance> instance, Config config) {
            var initial = instance.GetDbEntity().Item.Id;
            foreach (var edge in instance.GetChildMembers()) {
                var sameRelationWithEFCoreEntity = edge.Terminal
                    .GetDbEntity()
                    .In
                    .Single(e => e.RelationName == edge.RelationName)
                    .As<IEFCoreEntity>();
                var navigationProperty = new NavigationProperty(sameRelationWithEFCoreEntity, config);

                yield return new AggregateInstance.ChildProperty {
                    Owner = instance,
                    ChildAggregateInstance = edge.Terminal,
                    CorrespondingNavigationProperty = navigationProperty,
                    CSharpTypeName = edge.Terminal.Item.ClassName,
                    TypeScriptTypename = edge.Terminal.Item.TypeScriptTypeName,
                    PropertyName = edge.RelationName,
                };
            }
        }

        internal static IEnumerable<AggregateInstance.VariationSwitchProperty> GetVariationSwitchProperties(this GraphNode<AggregateInstance> instance, Config config) {
            var dbEntityColumns = instance
                .GetDbEntity()
                .GetColumns()
                .Where(col => col is IEFCoreEntity.VariationGroupTypeIdentifier)
                .Cast<IEFCoreEntity.VariationGroupTypeIdentifier>()
                .ToArray();

            foreach (var group in instance.GetVariationGroups()) {
                var correspondingDbColumn = dbEntityColumns.Single(col => col.PropertyName == group.GroupName);
                yield return new AggregateInstance.VariationSwitchProperty {
                    Owner = instance,
                    Group = group,
                    CorrespondingDbColumn = correspondingDbColumn,
                    CSharpTypeName = "string",
                    TypeScriptTypename = "string",
                    PropertyName = group.GroupName,
                    Config = config,
                };
            }
        }
        internal static IEnumerable<AggregateInstance.VariationProperty> GetVariationProperties(this GraphNode<AggregateInstance> node, Config config) {
            return node
                .GetVariationSwitchProperties(config)
                .SelectMany(group => group.GetGroupItems());
        }
        internal static IEnumerable<AggregateInstance.RefProperty> GetRefProperties(this GraphNode<AggregateInstance> instance, Config config) {
            foreach (var edge in instance.GetRefMembers()) {
                var initialDbEntity = edge.Initial.GetDbEntity();
                var terminalDbEntity = edge.Terminal.GetDbEntity();
                var edgeAsEfCore = terminalDbEntity.In
                    .Single(x => x.RelationName == edge.RelationName
                              && x.Initial.As<IEFCoreEntity>() == initialDbEntity)
                    .As<IEFCoreEntity>();

                yield return new AggregateInstance.RefProperty {
                    Owner = instance,
                    RefTarget = edge.Terminal,
                    CSharpTypeName = AggregateInstanceKeyNamePair.CLASSNAME,
                    TypeScriptTypename = AggregateInstanceKeyNamePair.TS_DEF,
                    PropertyName = edge.RelationName,
                    CorrespondingNavigationProperty = new NavigationProperty(edgeAsEfCore, config),
                    CorrespondingDbColumns = instance
                        .GetDbEntity()
                        .GetColumns()
                        .Where(col => col is IEFCoreEntity.RefTargetTablePrimaryKey refTargetPk
                                   && refTargetPk.Relation.Terminal == terminalDbEntity)
                        .ToArray(),
                };
            }
        }
    }
}
