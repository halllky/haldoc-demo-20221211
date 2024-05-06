using Nijo.Core;
using Nijo.Core.AggregateMemberTypes;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.Storing {
    /// <summary>
    /// 画面に表示されるデータの形。
    /// データ自身が持っている情報に加え、それがDBに存在するデータか否か、
    /// DBから読み込んだあと変更が加わっているか、などの画面に表示するために必要な情報も保持している。
    /// </summary>
    internal class DisplayDataClass {
        internal DisplayDataClass(GraphNode<Aggregate> aggregate) {
            MainAggregate = aggregate;
        }
        internal GraphNode<Aggregate> MainAggregate { get; }

        internal string TsTypeName => $"{MainAggregate.Item.TypeScriptTypeName}DisplayData";

        internal const string OWN_MEMBERS = "own_members";
        /// <summary>
        /// リモートリポジトリに存在しないインスタンス（react-hook-formが作成したインスタンス）ならfalse or undefined,
        /// 存在するインスタンス（ユーザーが作成したインスタンス）ならtrue。
        /// UI上でChildrenの主キーが変更可能かどうかの制御、削除のアクションが起きた時の挙動などに使用
        /// </summary>
        internal const string EXISTS_IN_REMOTE_REPOS = "existsInRemoteRepository";
        /// <summary>
        /// 画面上で何らかの変更が加えられてから、リモートリポジトリで削除されるまでの間、trueになる
        /// </summary>
        internal const string WILL_BE_CHANGED = "willBeChanged";
        /// <summary>
        /// 画面上で削除が指示されてから、リモートリポジトリで削除されるまでの間、trueになる
        /// </summary>
        internal const string WILL_BE_DELETED = "willBeDeleted";
        internal const string LOCAL_REPOS_ITEMKEY = "localRepositoryItemKey";

        /// <summary>
        /// <see cref="OWN_MEMBERS"/> 構造体の中に宣言されるプロパティを列挙します。
        /// </summary>
        internal IEnumerable<OwnProp> GetOwnProps() {
            return MainAggregate
                .GetMembers()
                .Where(m => m.DeclaringAggregate == MainAggregate
                         && (m is AggregateMember.ValueMember || m is AggregateMember.Ref))
                .Select(m => new OwnProp(MainAggregate, m));
        }
        internal IEnumerable<RelationProp> GetChildProps() {
            var childMembers = MainAggregate
                .GetMembers()
                .OfType<AggregateMember.RelationMember>()
                .Where(m => m is not AggregateMember.Ref
                         && m is not AggregateMember.Parent)
                .Select(m => new RelationProp(m.Relation, m));
            foreach (var item in childMembers) {
                yield return item;
            }
        }

        /// <summary>
        /// 新規オブジェクト作成のリテラルをレンダリングします。
        /// </summary>
        /// <param name="itemKey">ルート集約なら必須。子孫集約なら不要</param>
        internal string RenderNewObjectLiteral(string? itemKey = null) {
            return $$"""
                {
                {{If(MainAggregate.IsRoot(), () => $$"""
                  {{LOCAL_REPOS_ITEMKEY}}: {{itemKey}},
                  {{EXISTS_IN_REMOTE_REPOS}}: false,
                  {{WILL_BE_CHANGED}}: true,
                  {{WILL_BE_DELETED}}: false,
                """)}}
                  {{OWN_MEMBERS}}: {
                {{MainAggregate.GetMembers().OfType<AggregateMember.Schalar>().Where(m => m.DeclaringAggregate == MainAggregate && m.Options.MemberType is Uuid).SelectTextTemplate(m => $$"""
                    {{m.MemberName}}: UUID.generate(),
                """)}}
                {{MainAggregate.GetMembers().OfType<AggregateMember.Variation>().Where(m => m.DeclaringAggregate == MainAggregate).SelectTextTemplate(m => $$"""
                    {{m.MemberName}}: '{{m.GetGroupItems().First().Key}}',
                """)}}
                {{If(MainAggregate.IsChildrenMember(), () => $$"""
                    {{TransactionScopeDataClass.IS_STORED_DATA}}: false,
                """)}}
                  },
                }
                """;
        }

        internal string ConvertFnNameToLocalRepositoryType => $"convert{MainAggregate.Item.ClassName}ToLocalRepositoryItem";
        internal string ConvertFnNameToDisplayDataType => $"convert{MainAggregate.Item.ClassName}ToDisplayData";

        /// <summary>
        /// データ型変換関数 (<see cref="DisplayDataClass"/> => <see cref="TransactionScopeDataClass"/>)
        /// </summary>
        internal string RenderConvertFnToLocalRepositoryType() {

            string RenderItem(DisplayDataClass dc, string instance) {

                string RenderOwnMemberValue(AggregateMember.AggregateMemberBase member) {
                    if (member is AggregateMember.RelationMember refTarget) {
                        var keyArray = KeyArray.Create(refTarget.MemberAggregate);
                        var keyArrayType = $"[{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}]";

                        string RenderRefTargetKeyNameValue(AggregateMember.RelationMember refOrParent) {
                            var keyname = new RefTargetKeyName(refOrParent.MemberAggregate);
                            return $$"""
                                {
                                {{keyname.GetOwnKeyMembers().SelectTextTemplate(m => m is AggregateMember.RelationMember refOrParent2 ? $$"""
                                  {{m.MemberName}}: {{WithIndent(RenderRefTargetKeyNameValue(refOrParent2), "  ")}},
                                """ : $$"""
                                  {{m.MemberName}}: {{instance}}.{{refTarget.GetFullPathAsSingleViewDataClass().Join("?.")}}
                                    ? (JSON.parse({{instance}}.{{refTarget.GetFullPathAsSingleViewDataClass().Join(".")}}) as {{keyArrayType}})[{{keyArray.Single(k => k.Member.Declared == ((AggregateMember.ValueMember)m).Declared).Index}}]
                                    : undefined,
                                """)}}
                                }
                                """;
                        }
                        return RenderRefTargetKeyNameValue(refTarget);

                    } else {
                        return $$"""
                            {{instance}}?.{{member.GetFullPathAsSingleViewDataClass().Join("?.")}}
                            """;
                    }
                }
                return $$"""
                    {
                    {{dc.GetOwnProps().SelectTextTemplate(p => $$"""
                      {{p.Member.MemberName}}: {{WithIndent(RenderOwnMemberValue(p.Member), "  ")}},
                    """)}}
                    {{dc.GetChildProps().SelectTextTemplate(p => p.MemberInfo is AggregateMember.Children ? $$"""
                      {{p.MemberInfo?.MemberName}}: {{instance}}.{{p.MemberInfo?.MemberAggregate.GetFullPathAsSingleViewDataClass().Join("?.")}}?.map(x{{p.MemberInfo?.MemberName}} => ({{WithIndent(RenderItem(new DisplayDataClass(p.MainAggregate.AsEntry()), $"x{p.MemberInfo?.MemberName}"), "  ")}})),
                    """ : $$"""
                      {{p.MemberInfo?.MemberName}}: {{WithIndent(RenderItem(p, instance), "  ")}},
                    """)}}
                    {{If(dc.MainAggregate.IsChildrenMember(), () => $$"""
                      {{TransactionScopeDataClass.IS_STORED_DATA}}: {{instance}}.{{OWN_MEMBERS}}.{{TransactionScopeDataClass.IS_STORED_DATA}},
                    """)}}
                    }
                    """;
            }

            return $$"""
                /** 画面に表示されるデータ型を登録更新される粒度の型に変換します。 */
                export const {{ConvertFnNameToLocalRepositoryType}} = (displayData: {{TsTypeName}}) => {
                  const item0: Util.LocalRepositoryItem<{{MainAggregate.Item.TypeScriptTypeName}}> = {
                    itemKey: displayData.{{LOCAL_REPOS_ITEMKEY}},
                    existsInRemoteRepository: displayData.{{EXISTS_IN_REMOTE_REPOS}},
                    willBeChanged: displayData.{{WILL_BE_CHANGED}},
                    willBeDeleted: displayData.{{WILL_BE_DELETED}},
                    item: {{WithIndent(RenderItem(this, "displayData"), "    ")}},
                  }

                  return [
                    item0,
                  ] as const
                }
                """;
        }

        /// <summary>
        /// データ型変換関数 (<see cref="TransactionScopeDataClass"/> => <see cref="DisplayDataClass"/>)
        /// </summary>
        internal string RenderConvertFnToDisplayDataClass() {
            var mainArgName = $"reposItem{MainAggregate.Item.ClassName}";
            var mainArgType = $"Util.LocalRepositoryItem<{MainAggregate.Item.TypeScriptTypeName}>";

            // 子孫要素を参照するデータを引数の配列中から探すためにはキーで引き当てる必要があるが、
            // 子孫要素のラムダ式の中ではその外にある変数を参照するしかない
            var pkVarNames = new Dictionary<AggregateMember.ValueMember, string>();
            foreach (var key in MainAggregate.GetKeys().OfType<AggregateMember.ValueMember>()) {
                pkVarNames.Add(key.Declared, $"{mainArgName}.item.{key.Declared.GetFullPath().Join("?.")}");
            }

            string Render(DisplayDataClass dc, string instance, bool inLambda) {
                var keys = inLambda
                    ? dc.MainAggregate.AsEntry().GetKeys().OfType<AggregateMember.ValueMember>()
                    : dc.MainAggregate.GetKeys().OfType<AggregateMember.ValueMember>();

                foreach (var key in keys) {
                    // 実際にはここでcontinueされるのは親のキーだけのはず。Render関数はルートから順番に呼び出されるので
                    if (pkVarNames.ContainsKey(key.Declared)) continue;

                    pkVarNames.Add(key.Declared, $"{instance}.{key.Declared.GetFullPath().Join("?.")}");
                }

                var ownMembers = dc.MainAggregate
                    .AsEntry()
                    .GetMembers()
                    .Where(m => m.DeclaringAggregate == dc.MainAggregate
                             && (m is AggregateMember.ValueMember || m is AggregateMember.Ref));
                var item = dc.MainAggregate.IsRoot() ? $"{instance}.item" : instance;
                var depth = dc.MainAggregate.EnumerateAncestors().Count();

                string MemberValue(AggregateMember.AggregateMemberBase m) {
                    if (m is AggregateMember.Ref @ref) {
                        var keys = @ref.MemberAggregate
                            .GetKeys()
                            .OfType<AggregateMember.ValueMember>();
                        return $$"""
                            {{item}}?.{{m.MemberName}}
                              ? JSON.stringify([{{keys.Select(k => $"{item}.{k.Declared.GetFullPath().Join("?.")}").Join(", ")}}]) as Util.ItemKey
                              : undefined
                            """;

                    } else {
                        return $"{item}?.{m.MemberName}";
                    }
                }

                return $$"""
                    {
                    {{If(dc.MainAggregate.IsRoot(), () => $$"""
                      {{LOCAL_REPOS_ITEMKEY}}: {{instance}}.itemKey,
                      {{EXISTS_IN_REMOTE_REPOS}}: {{instance}}.existsInRemoteRepository,
                      {{WILL_BE_CHANGED}}: {{instance}}.willBeChanged,
                      {{WILL_BE_DELETED}}: {{instance}}.willBeDeleted,
                    """)}}
                      {{OWN_MEMBERS}}: {
                    {{ownMembers.SelectTextTemplate(m => $$"""
                        {{m.MemberName}}: {{WithIndent(MemberValue(m), "    ")}},
                    """)}}
                    {{If(dc.MainAggregate.IsChildrenMember(), () => $$"""
                        {{TransactionScopeDataClass.IS_STORED_DATA}}: {{item}}?.{{TransactionScopeDataClass.IS_STORED_DATA}} ?? false,
                    """)}}
                      },
                    {{dc.GetChildProps().SelectTextTemplate(p => p.IsArray ? $$"""
                      {{p.PropName}}: {{item}}?.{{p.MemberInfo?.MemberName}}?.map(x{{depth}} => ({{WithIndent(Render(p, $"x{depth}", true), "  ")}})),
                    """ : $$"""
                      {{p.PropName}}: {{WithIndent(Render(p, $"{item}?.{p.MemberInfo?.MemberName}", false), "  ")}},
                    """)}}
                    }
                    """;
            }

            return $$"""
                /** 登録更新される型を画面に表示されるデータ型に変換します。 */
                export const {{ConvertFnNameToDisplayDataType}} = (
                  {{mainArgName}}: {{mainArgType}},
                ): {{TsTypeName}} => {

                  return {{WithIndent(Render(this, mainArgName, false), "  ")}}
                }
                """;
        }

        internal string RenderTypeScriptDataClassDeclaration() {
            if (!MainAggregate.IsRoot()) throw new InvalidOperationException();

            return MainAggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => {
                var dataClass = new DisplayDataClass(agg);

                return $$"""
                    /** {{agg.Item.DisplayName}}の画面表示用データ */
                    export type {{dataClass.TsTypeName}} = {
                    {{If(agg.IsRoot(), () => $$"""
                      {{LOCAL_REPOS_ITEMKEY}}: Util.ItemKey
                      {{EXISTS_IN_REMOTE_REPOS}}: boolean
                      {{WILL_BE_CHANGED}}: boolean
                      {{WILL_BE_DELETED}}: boolean
                    """)}}
                      {{OWN_MEMBERS}}: {
                    {{dataClass.GetOwnProps().SelectTextTemplate(p => $$"""
                        {{p.PropName}}?: {{p.PropType}}
                    """)}}
                    {{If(agg.IsChildrenMember(), () => $$"""
                        {{TransactionScopeDataClass.IS_STORED_DATA}}: boolean,
                    """)}}
                      }
                    {{dataClass.GetChildProps().SelectTextTemplate(p => $$"""
                      {{p.PropName}}?: {{(p.IsArray ? $"{new DisplayDataClass(p.MainAggregate).TsTypeName}[]" : new DisplayDataClass(p.MainAggregate).TsTypeName)}}
                    """)}}
                    }
                    """;
            });
        }

        internal class OwnProp {
            internal OwnProp(GraphNode<Aggregate> dataClassMainAggregate, AggregateMember.AggregateMemberBase member) {
                _mainAggregate = dataClassMainAggregate;
                Member = member;
            }
            private readonly GraphNode<Aggregate> _mainAggregate;
            internal AggregateMember.AggregateMemberBase Member { get; }

            internal string PropName => Member.MemberName;
            internal string PropType => Member is AggregateMember.Ref
                ? "Util.ItemKey"
                : Member.TypeScriptTypename;
        }

        internal class RelationProp : DisplayDataClass {
            internal RelationProp(GraphEdge<Aggregate> relation, AggregateMember.RelationMember? memberInfo)
                : base(relation.IsRef() ? relation.Initial : relation.Terminal) {
                MemberInfo = memberInfo;
            }
            /// <summary>
            /// Ref From プロパティの場合はnull
            /// </summary>
            internal AggregateMember.RelationMember? MemberInfo { get; }

            internal enum E_Type {
                Descendant,
                RefFrom,
            }
            internal E_Type Type => MainAggregate.Source?.IsParentChild() == true
                ? E_Type.Descendant
                : E_Type.RefFrom;

            /// <summary>
            /// 従属集約が保管されるプロパティの名前を返します
            /// </summary>
            internal string PropName {
                get {
                    if (MainAggregate.Source == null) {
                        throw new InvalidOperationException("ルート集約のPropは考慮していない");

                    } else if (MainAggregate.Source.IsParentChild()) {
                        return $"child_{MainAggregate.Item.ClassName}";

                    } else {
                        return $"ref_from_{MainAggregate.Source.RelationName.ToCSharpSafe()}_{MainAggregate.Item.ClassName}";
                    }
                }
            }
            /// <summary>
            /// 主たる集約またはそれと1対1の多重度にある集約であればfalse
            /// </summary>
            internal bool IsArray => MemberInfo is AggregateMember.Children;
        }
    }

    internal static partial class StoringExtensions {

        #region DisplayDataClassのパス
        /// <summary>
        /// エントリーからのパスを <see cref="DisplayDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="DisplayDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since = null) {
            return GetFullPathAsSingleViewDataClass(aggregate, since, out var _);
        }
        /// <summary>
        /// エントリーからのパスを <see cref="DisplayDataClass"/> のデータ構造にあわせて返す。
        /// たとえば自身のメンバーならその前に <see cref="DisplayDataClass.OWN_MEMBERS"/> を挟むなど
        /// </summary>
        internal static IEnumerable<string> GetFullPathAsSingleViewDataClass(this AggregateMember.AggregateMemberBase member, GraphNode<Aggregate>? since = null) {
            bool enumeratingRefTargetKeyName;
            foreach (var path in GetFullPathAsSingleViewDataClass(member.Owner, since, out enumeratingRefTargetKeyName)) {
                yield return path;
            }
            if (!enumeratingRefTargetKeyName) {
                yield return DisplayDataClass.OWN_MEMBERS;
            }
            yield return member.MemberName;
        }
        private static IEnumerable<string> GetFullPathAsSingleViewDataClass(this GraphNode<Aggregate> aggregate, GraphNode<Aggregate>? since, out bool enumeratingRefTargetKeyName) {
            var paths = new List<string>();
            enumeratingRefTargetKeyName = false;

            var pathFromEntry = aggregate.PathFromEntry();
            if (since != null) pathFromEntry = pathFromEntry.Since(since);

            foreach (var edge in pathFromEntry) {
                if (edge.Source == edge.Terminal) {

                    if (edge.IsParentChild()) {
                        paths.Add(AggregateMember.PARENT_PROPNAME); // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {
                        enumeratingRefTargetKeyName = false;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {

                    if (edge.IsParentChild()) {
                        var dc = new DisplayDataClass(edge.Initial.As<Aggregate>());
                        paths.Add(dc
                            .GetChildProps()
                            .Single(p => p.MainAggregate == edge.Terminal.As<Aggregate>())
                            .PropName);

                        enumeratingRefTargetKeyName = false;

                    } else if (edge.IsRef()) {
                        if (!enumeratingRefTargetKeyName) {
                            paths.Add(DisplayDataClass.OWN_MEMBERS);
                        }

                        /// <see cref="RefTargetKeyName"/> の仕様に合わせる
                        paths.Add(edge.RelationName);

                        enumeratingRefTargetKeyName = true;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の元から先に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }

            return paths;
        }
        #endregion DisplayDataClassのパス


        #region useFormContextのパス
        /// <summary>
        /// React Hook Form の記法に従ったルートオブジェクトからの登録名のパスを返します。
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetRHFRegisterName(this GraphNode<Aggregate> aggregate, IEnumerable<string>? arrayIndexes = null) {
            foreach (var path in EnumerateRHFRegisterName(aggregate, false, arrayIndexes)) {
                yield return path;
            }
        }
        /// <summary>
        /// React Hook Form の記法に従ったルートオブジェクトからの登録名のパスを返します。
        /// </summary>
        /// <param name="arrayIndexes">配列インデックスを指定する変数の名前</param>
        internal static IEnumerable<string> GetRHFRegisterName(this AggregateMember.AggregateMemberBase member, IEnumerable<string>? arrayIndexes = null) {
            foreach (var path in EnumerateRHFRegisterName(member.Owner, true, arrayIndexes)) {
                yield return path;
            }
            yield return DisplayDataClass.OWN_MEMBERS;
            yield return member.MemberName;
        }

        private static IEnumerable<string> EnumerateRHFRegisterName(this GraphNode<Aggregate> aggregate, bool enumerateLastChildrenIndex, IEnumerable<string>? arrayIndexes) {
            var currentArrayIndex = 0;

            foreach (var edge in aggregate.PathFromEntry()) {
                if (edge.Source == edge.Terminal) {

                    if (edge.IsParentChild()) {
                        yield return AggregateMember.PARENT_PROPNAME; // 子から親に向かって辿る場合

                    } else if (edge.IsRef()) {

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }

                } else {

                    if (edge.IsParentChild()) {
                        var dataClass = new DisplayDataClass(edge.Initial.As<Aggregate>());
                        var terminal = edge.Terminal.As<Aggregate>();

                        yield return dataClass
                            .GetChildProps()
                            .Single(p => p.MainAggregate == terminal)
                            .PropName;

                        // 子要素が配列の場合はその配列の何番目の要素かを指定する必要がある
                        if (terminal.IsChildrenMember()
                            // "….Children.${}" の最後の配列インデックスを列挙するか否か
                            && (enumerateLastChildrenIndex || terminal != aggregate)) {

                            var arrayIndex = arrayIndexes?.ElementAtOrDefault(currentArrayIndex);
                            yield return $"${{{arrayIndex}}}";

                            currentArrayIndex++;
                        }

                    } else if (edge.IsRef()) {
                        yield return edge.RelationName;

                    } else {
                        throw new InvalidOperationException($"有向グラフの矢印の先から元に向かうパターンは親子か参照だけなのでこの分岐にくることはあり得ないはず");
                    }
                }
            }
        }
        #endregion useFormContextのパス
    }
}
