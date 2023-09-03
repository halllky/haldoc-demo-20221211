using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.WebClient {
    partial class FormOfAggregateInstance {
        protected override string Template() {
            var components = _instance
                .EnumerateThisAndDescendants()
                .Select(x => x.IsChildrenMember() ? new ComponentOfChildrenListItem(x) : new Component(x));

            return $$"""
                import React, { useCallback } from 'react'
                import { PlusIcon, XMarkIcon } from '@heroicons/react/24/outline'
                import { useForm, useFieldArray, useFormContext } from 'react-hook-form'
                import { usePageContext } from '../../hooks/PageContext'
                import * as Components from '../../components'
                import * as AggregateType from '{{TypesImport}}'

                {{components.SelectTextTemplate(desc => $$"""
                export const {{desc.ComponentName}} = ({ {{GetArguments(desc.AggregateInstance).Values.Join(", ")}} }: {
                {{GetArguments(desc.AggregateInstance).Values.SelectTextTemplate(arg => $$"""
                  {{arg}}: number
                """)}}
                }) => {
                  const [{ pageIsReadOnly },] = usePageContext()
                  const { register, watch } = useFormContext<AggregateType.{{_instance.Item.TypeScriptTypeName}}>()

                  return <>
                {{desc.AggregateInstance.GetProperties(_ctx.Config).SelectTextTemplate(prop => $$"""
                {{TemplateTextHelper.If(prop is AggregateInstance.SchalarProperty, () => $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{prop.PropertyName}}
                        </span>
                      </div>
                      <div className="flex-1">
                        {{RenderSchalarProperty(desc.AggregateInstance, (AggregateInstance.SchalarProperty)prop, "        ")}}
                      </div>
                    </div>

                """).ElseIf(prop is AggregateInstance.RefProperty, () => $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{prop.PropertyName}}
                        </span>
                      </div>
                      <div className="flex-1">
                        {{TemplateTextHelper.WithIndent(RenderRefAggregateBody((AggregateInstance.RefProperty)prop), "        ")}}
                      </div>
                    </div>

                """).ElseIf(prop is AggregateInstance.ChildProperty, () => $$"""
                    <div className="py-2">
                      <span className="text-sm select-none opacity-80">
                        {{prop.PropertyName}}
                      </span>
                      <div className="flex flex-col space-y-1 p-1 border border-neutral-400">
                        {{TemplateTextHelper.WithIndent(RenderChildAggregateBody((AggregateInstance.ChildProperty)prop), "        ")}}
                      </div>
                    </div>

                """).ElseIf(prop is AggregateInstance.VariationProperty variationProperty
                         && variationProperty.Key == variationProperty.Group.VariationAggregates.First().Key, () => $$"""
                    <div className="flex">
                      <div className="{{PropNameWidth}}">
                        <span className="text-sm select-none opacity-80">
                          {{((AggregateInstance.VariationProperty)prop).Group.GroupName}}
                        </span>
                      </div>
                      <div className="flex-1 flex gap-2 flex-wrap">
                {{((AggregateInstance.VariationProperty)prop).Group.VariationAggregates.SelectTextTemplate(item => $$"""
                        <label>
                          <input type="radio" value="{{item.Key}}" disabled={pageIsReadOnly} {...register(`{{GetRegisterName(desc.AggregateInstance, (AggregateInstance.VariationProperty)prop).Value}}`)} />
                          {{item.Value.RelationName}}
                        </label>
                """)}}
                      </div>
                    </div>
                {{((AggregateInstance.VariationProperty)prop).Group.VariationAggregates.SelectTextTemplate(item => $$"""
                    <div className={`flex flex-col space-y-1 p-1 border border-neutral-400 ${(watch(`{{GetRegisterName(desc.AggregateInstance, (AggregateInstance.VariationProperty)prop).Value}}`) !== '{{item.Key}}' ? 'hidden' : '')}`}>
                      {{TemplateTextHelper.WithIndent(RenderVariationAggregateBody(item.Value.Terminal), "      ")}}
                    </div>
                """)}}

                """).ElseIf(prop is AggregateInstance.ChildrenProperty, () => $$"""
                    <div className="py-2">
                      <span className="text-sm select-none opacity-80">
                        {{prop.PropertyName}}
                      </span>
                      <div className="flex flex-col space-y-1">
                        {{TemplateTextHelper.WithIndent(RenderChildrenAggregateBody((AggregateInstance.ChildrenProperty)prop), "        ")}}
                      </div>
                    </div>

                """)}}
                """)}}
                  </>
                }


                {{TemplateTextHelper.If(desc.IsChildren, () => $$"""
                export const {{desc.ComponentName}} = (args: {
                {{GetArguments(desc.AggregateInstance).Values.SkipLast(1).SelectTextTemplate(arg => $$"""
                  {{arg}}: number
                """)}}
                }) => {
                  const [{ pageIsReadOnly },] = usePageContext()
                  const { control, register } = useFormContext<AggregateType.{{_instance.Item.TypeScriptTypeName}}>()
                  const { fields, append, remove } = useFieldArray({
                    control,
                    name: `{{desc.GetUseFieldArrayName()}}`,
                  })
                  const onAdd = useCallback((e: React.MouseEvent) => {
                    append(AggregateType.{{new types.AggregateInstanceInitializerFunction(desc.AggregateInstance).FunctionName}}())
                    e.preventDefault()
                  }, [append])

                  return (
                    <>
                      {fields.map((_, index) => (
                        <div key={index} className="flex flex-col space-y-1 p-1 border border-neutral-400">
                          <{{desc.ComponentName}}
                            {...args}
                            {{GetArguments(desc.AggregateInstance).Values.Last()}}={index}
                          />
                          {!pageIsReadOnly &&
                            <Components.IconButton
                              underline
                              icon={XMarkIcon}
                              onClick={e => { remove(index); e.preventDefault() }\}
                              className="self-start">削除</Components.IconButton>}
                        </div>
                      ))}
                      {!pageIsReadOnly &&
                        <Components.IconButton
                          underline
                          icon={PlusIcon}
                          onClick={onAdd}
                          className="self-start">
                          追加
                        </Components.IconButton>}
                    </>
                  )
                }
                """)}}
                """)}}
                """;
        }
    }
}
