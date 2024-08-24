import React, { useMemo } from 'react'

const DEFAULT_INDENT_SIZE = '1.5rem'
const DEFAULT_LABEL_WIDTH = '8rem'


const Root = ({ estimatedLabelWidth, indentSize, maxDepth, children }: {
  /** フォーム内のラベルの横幅のうち最も大きいもの */
  estimatedLabelWidth?: string
  /** 子孫要素の部分のインデントの大きさ */
  indentSize?: string
  /** 最も深い子孫要素の深さ */
  maxDepth?: number
  children?: React.ReactNode
}) => {
  const rootStyle = useMemo((): React.CSSProperties => ({
    // CSSのコンテナクエリ (@container) を使うためにはその親要素に
    // container-type が指定されている必要がある
    containerType: 'inline-size',

    // デフォルトのCSSファイルの中でこれらのCSS変数を使ってラベル列の横幅を計算している
    // @ts-ignore
    '--vform-max-depth': maxDepth?.toString() ?? '0',
    '--vform-indent-size': indentSize ?? DEFAULT_INDENT_SIZE,
    '--vform-label-width': estimatedLabelWidth ?? DEFAULT_LABEL_WIDTH,
  }), [maxDepth, indentSize, estimatedLabelWidth])

  return (
    <div style={rootStyle}>
      <div className="grid gap-px w-full h-full vform-template-column border-vform bg-color-2">
        {children}
      </div>
    </div>
  )
}


const AutoColumn = ({ children }: { children?: React.ReactNode }) => {
  // デフォルトのCSSファイルでこのクラス名に合った grid-template-rows が定義されています。
  // 例えば要素の数が3個のとき、以下5パターンそれぞれのコンテナクエリが定義されています。
  // - 画面の横幅が1列しか収まらない場合: 要素の縦方向の最大数は3個
  // - 画面の横幅が2列収まるサイズの場合: 要素の縦方向の最大数は2個 (3 ÷ 2 を切り上げ)
  // - 画面の横幅が3列以上収まる幅の場合: 要素の縦方向の最大数は1個 (3 ÷ 3)
  const className = useMemo(() => {
    const count = React.Children.count(children)
    return `grid gap-px grid-cols-[subgrid] col-span-full grid-flow-col vform-vertical-${count}-items`
  }, [children])

  return (
    <div className={className}>
      {children}
    </div>
  )
}


const Indent = ({ label, children }: {
  label?: React.ReactNode
  children?: React.ReactNode
}) => {
  return (
    <div className="grid grid-cols-[subgrid] col-span-full border-vform">
      <div className="px-1 col-span-full select-none">
        {renderLabel(label)}&nbsp;
      </div>
      <div className="grid gap-px grid-cols-[subgrid] col-span-full pl-[var(--vform-indent-size)]">
        {children}
      </div>
    </div>
  )
}

const Item = ({ label, wide, children }: {
  label?: React.ReactNode
  /** trueの場合は要素が横幅いっぱいまで拡張される */
  wide?: boolean
  children?: React.ReactNode
}) => {
  return wide
    // 要素がグリッドの横幅いっぱい確保される場合のレイアウト
    ? (
      <>
        <div className="px-1 col-span-full border-vform">
          {renderLabel(label)}
        </div>
        <div className="col-span-full border-vform bg-color-0">
          {children}
        </div>
      </>
    )
    // 要素の横幅が小さい場合のレイアウト
    : (
      <div className="grid grid-flow-row grid-cols-[subgrid] col-span-2">
        <div className="p-px border-vform">
          {renderLabel(label)}
        </div>
        <div className="p-px border-vform bg-color-0">
          {children}
        </div>
      </div>
    )
}


const LabelText = ({ children }: {
  children?: React.ReactNode
}) => {
  return (
    <span className="select-none text-color-7 text-sm font-semibold">
      {children}
    </span>
  )
}

/** 外から受け取ったラベルが文字列型など単純な型の場合はLabelTextコンポーネントでラップする */
const renderLabel = (label: React.ReactNode): React.ReactNode => {
  const t = typeof label
  if (t === 'string' || t === 'number' || t === 'bigint') {
    return <LabelText>{label}</LabelText>
  } else {
    return label
  }
}

export const VForm2 = {
  /** フォームレイアウト */
  Root,
  /** この要素の中にItemを並べると、コンテナの横幅にあわせて自動的に段組みされます。 */
  AutoColumn,
  /** 入れ子セクション */
  Indent,
  /** フォームの要素のラベルと値 */
  Item,
  /** フォームのラベル */
  LabelText,
}
