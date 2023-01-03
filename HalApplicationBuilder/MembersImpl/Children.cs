﻿using System;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;

namespace HalApplicationBuilder.MembersImpl {
    internal class Children : AggregateMemberBase {
        public override bool IsCollection => true;

        private Aggregate _child;
        internal Aggregate ChildAggregate {
            get {
                if (_child == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _child = new Aggregate(type, this, MemberFactory);
                }
                return _child;
            }
        }

        public override IEnumerable<Core.Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels(IAggregateMember member) {
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = $"List<{Schema.GetInstanceModel(ChildAggregate).RuntimeFullName}>",
                PropertyName = Name,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels(IAggregateMember member) {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels(IAggregateMember member) {
            yield break;
        }

        internal override string RenderSearchConditionView(ViewRenderingContext context) {
            return string.Empty;
        }

        internal override string RenderSearchResultView(ViewRenderingContext context) {
            return string.Empty;
        }

        internal override string RenderInstanceView(ViewRenderingContext context) {
            var model = InstanceModels.Single();
            var nested = context.Nest(model.PropertyName, isCollection: true);
            var template = new ChildrenInstanceTemplate {
                i = context.LoopVar,
                Count = $"{nested.CollectionPath}.{nameof(ICollection<object>.Count)}",
                PartialViewName = new InstancePartialView(ChildAggregate, Config).FileName,
                PartialViewBoundObjectName = nested.AspForPath,
                AspForAddChild = nested.AspForCollectionPath,
            };
            return template.TransformText();
        }
    }

    partial class ChildrenInstanceTemplate {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<意図して小文字>")]
        internal string i { get; set; }
        internal string Count { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
        internal string AspForAddChild { get; set; }

        internal static string AddButtonSenderIdentifier => AspNetMvc.JsTemplate.AGGREGATE_TREE_PATH_ATTR;
        internal static string AddButtonCssClass => AspNetMvc.JsTemplate.ADD_CHILD_BTN;
    }
}
