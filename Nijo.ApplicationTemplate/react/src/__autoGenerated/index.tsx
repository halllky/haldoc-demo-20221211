import React, { useMemo } from 'react'
import { createBrowserRouter, Link, NavLink, Outlet, RouteObject, RouterProvider, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from 'react-query'
import * as Icon from '@heroicons/react/24/outline'
import { ImperativePanelHandle, Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { DialogContextProvider } from './collection'
import * as Util from './util'
import * as AutoGenerated from './autogenerated-menu'
import { UiContext, UiContextProvider } from './default-ui-component'
import DashBoard from './pages/DashBoard'

export * from './collection'
export * from './input'
export * from './util'

import './nijo-default-style.css'
import { UiCustomizer } from './autogenerated-types'

/** 自動生成されるソースとその外側との境界 */
export function DefaultNijoApp(props: DefaultNijoAppProps) {
  const { useRouteCustomizer } = props

  // ルーティング設定
  const modifyRoutes = useRouteCustomizer?.()
  const router = useMemo(() => {
    const defaultRoutes: RouteObject[] = [
      { path: '/', element: <DashBoard applicationName={props.applicationName} /> },
      { path: '/settings', element: <Util.ServerSettingScreen /> },
      { path: '*', element: <p> Not found.</p> },
      ...AutoGenerated.routes,
    ]
    if (AutoGenerated.SHOW_LOCAL_REPOSITORY_MENU) {
      defaultRoutes.push({ path: '/changes', element: <Util.LocalReposChangeListPage /> })
    }

    return createBrowserRouter([{
      path: '/',
      children: modifyRoutes?.(defaultRoutes) ?? defaultRoutes,
      element: (
        <QueryClientProvider client={queryClient}>
          <Util.MsgContextProvider>
            <Util.ToastContextProvider>
              <Util.LocalRepositoryContextProvider>
                <Util.UserSettingContextProvider>
                  <UiContextProvider customizer={props.uiCustomizer}>
                    <DialogContextProvider>
                      <ApplicationRootInContext {...props} />
                      <Util.EnvNameRibbon />
                      <Util.Toast />
                    </DialogContextProvider>
                  </UiContextProvider>
                </Util.UserSettingContextProvider>
              </Util.LocalRepositoryContextProvider>
            </Util.ToastContextProvider>
          </Util.MsgContextProvider>
        </QueryClientProvider>
      ),
    },
    ])
  }, [modifyRoutes, ...Object.values(props)])

  return (
    <RouterProvider router={router} />
  )
}

/** DefaultNijoAppのプロパティ */
export type DefaultNijoAppProps = {
  /** UIカスタマイザ。自動生成されたUIをカスタマイズする場合はこの関数の中で定義してください。 */
  uiCustomizer: UiCustomizer
  /** アプリケーション名 */
  applicationName: string
  /** Raect router のルーティング処理（クライアント側のURLとページの紐づき設定）を編集するReactフック */
  useRouteCustomizer?: () => ((defaultRoutes: RouteObject[]) => RouteObject[])
}

/** useQueryを使うために必須 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})

/** アプリケーション本体 */
function ApplicationRootInContext({
  applicationName,
}: DefaultNijoAppProps) {
  // ユーザー設定
  const { data: {
    darkMode,
    fontFamily,
  } } = Util.useUserSetting()

  // 変更（ローカルリポジトリ）
  const { changesCount } = Util.useLocalRepositoryChangeList()

  // サイドメニュー
  const { LoginPage, useSideMenuCustomizer } = React.useContext(UiContext)
  const sideMenuCustomizer = useSideMenuCustomizer?.()

  // サイドメニュー開閉
  const sideMenuRef = React.useRef<ImperativePanelHandle>(null)
  const sideMenuContextValue = React.useMemo((): Util.SideMenuContextType => ({
    toggle: () => sideMenuRef.current?.getCollapsed()
      ? sideMenuRef.current.expand()
      : sideMenuRef.current?.collapse(),
    setCollapsed: collapsed => collapsed
      ? sideMenuRef.current?.collapse()
      : sideMenuRef.current?.expand(),
  }), [sideMenuRef])

  const [sideMenu, setSideMenu] = React.useState<{ top: Util.TreeNode<AutoGenerated.SideMenuItem>[], bottom: Util.TreeNode<AutoGenerated.SideMenuItem>[] }>({ top: [], bottom: [] })
  const fetchSideMenu = React.useCallback(async () => {

    // 既定で生成されるメニュー
    const top = AutoGenerated.menuItems
    const bottom: AutoGenerated.SideMenuItem[] = []
    if (AutoGenerated.SHOW_LOCAL_REPOSITORY_MENU) bottom.push({
      url: '/changes',
      icon: Icon.ArrowsUpDownIcon,
      text: changesCount === 0
        ? <span>一時保存</span>
        : <span className="font-bold">一時保存&nbsp;({changesCount})</span>
    })
    bottom.push({
      url: '/settings',
      text: '設定',
      icon: Icon.Cog8ToothIcon,
    })

    // メニューがカスタマイズされる場合はその処理を通す
    const customizedMenu = sideMenuCustomizer
      ? await sideMenuCustomizer({ top, bottom })
      : { top, bottom }

    // メニューの階層構造を計算する
    setSideMenu({
      top: Util.flatten(Util.toTree(customizedMenu.top, { getId: x => x.url, getChildren: x => x.children })),
      bottom: Util.flatten(Util.toTree(customizedMenu.bottom, { getId: x => x.url, getChildren: x => x.children })),
    })
  }, [sideMenuCustomizer])

  React.useEffect(() => {
    fetchSideMenu()
  }, [fetchSideMenu])

  return (
    <LoginPage LoggedInContents={(
      <Util.SideMenuContext.Provider value={sideMenuContextValue}>
        <PanelGroup
          direction='horizontal'
          autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_X"
          className={darkMode ? 'dark' : undefined}
          style={{ fontFamily: fontFamily ?? Util.DEFAULT_FONT_FAMILY }}>

          {/* サイドメニュー */}
          <Panel ref={sideMenuRef} defaultSize={20} collapsible>
            <PanelGroup direction="vertical"
              className="bg-color-2 text-color-12"
              autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_Y">
              <Panel className="flex flex-col">
                <Link to='/' className="p-1 ellipsis-ex font-semibold select-none border-r border-color-4">
                  {applicationName}
                </Link>
                <nav className="flex-1 overflow-y-auto leading-none flex flex-col">
                  {sideMenu.top.map(x =>
                    <SideMenuLink key={x.item.url} url={x.item.url} depth={x.depth} icon={x.item.icon}>{x.item.text}</SideMenuLink>
                  )}
                  <div className="flex-1 min-h-0 border-r border-color-4"></div>
                </nav>
                <nav className="flex flex-col">
                  {sideMenu.bottom.map(x =>
                    <SideMenuLink key={x.item.url} url={x.item.url} depth={x.depth} icon={x.item.icon}>{x.item.text}</SideMenuLink>
                  )}
                </nav>
                <span className="p-1 text-sm whitespace-nowrap overflow-hidden border-r border-color-4">
                  ver. 0.9.0.0
                </span>
              </Panel>
            </PanelGroup>
          </Panel>

          <PanelResizeHandle className="w-1 bg-color-base" />

          {/* コンテンツ */}
          <Panel className={`flex flex-col bg-color-base text-color-12`}>
            <Util.MsgContextProvider>

              {/* createBrowserRouterのchildrenのうち現在のURLと対応するものがOutletの位置に表示される */}
              <Outlet />

            </Util.MsgContextProvider>

            {/* コンテンツの外で発生したエラーが表示される欄 */}
            <Util.InlineMessageList />

          </Panel>
        </PanelGroup>
      </Util.SideMenuContext.Provider>
    )} />
  )
}

const SideMenuLink = ({ url, icon, depth, children }: {
  url: string
  icon?: React.ElementType
  depth?: number
  children?: React.ReactNode
}) => {

  // このメニューのページが開かれているかどうかでレイアウトを分ける
  const location = useLocation()
  const className = location.pathname.startsWith(url)
    ? 'flex-none outline-none inline-block w-full p-1 ellipsis-ex border-y border-color-4 bg-color-base font-bold'
    : 'flex-none outline-none inline-block w-full p-1 ellipsis-ex border-r border-color-4 my-px'

  // インデント
  const style: React.CSSProperties = {
    paddingLeft: depth === undefined ? undefined : `${depth * 1.2}rem`,
  }

  return (
    <NavLink to={url} className={className} style={style}>
      {React.createElement(icon ?? Icon.CircleStackIcon, { className: 'inline w-4 mr-1 opacity-70 align-middle' })}
      <span className="text-sm align-middle select-none">{children}</span>
    </NavLink>
  )
}
