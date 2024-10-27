import React, { useMemo } from 'react'
import { createBrowserRouter, Link, NavLink, Outlet, RouteObject, RouterProvider, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from 'react-query'
import * as Icon from '@heroicons/react/24/outline'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import { DialogContextProvider } from './collection'
import * as Util from './util'
import * as AutoGenerated from './autogenerated-menu'
import { AutoGeneratedCustomizer, createNewCustomizerContext, useCustomizerContext } from './autogenerated-customizer'
import { CustomizerContextProvider } from './autogenerated-customizer'
import DashBoard from './pages/DashBoard'

export * from './collection'
export * from './input'
export * from './util'
export * from './autogenerated-customizer'

import './nijo-default-style.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})

function ApplicationRootInContext() {
  // ユーザー設定
  const { data: {
    darkMode,
    fontFamily,
  } } = Util.useUserSetting()

  // 変更（ローカルリポジトリ）
  const { changesCount } = Util.useLocalRepositoryChangeList()

  // サイドメニュー
  const { applicationName, useSideMenuCustomizer } = useCustomizerContext()
  const sideMenuCustomizer = useSideMenuCustomizer?.()
  const [{ collapsed }] = Util.useSideMenuContext()

  const sideMenu = useMemo(() => {

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
    const customizedMenu = sideMenuCustomizer?.({ top, bottom }) ?? { top, bottom }

    // メニューの階層構造を計算する
    return {
      top: Util.flatten(Util.toTree(customizedMenu.top, { getId: x => x.url, getChildren: x => x.children })),
      bottom: Util.flatten(Util.toTree(customizedMenu.bottom, { getId: x => x.url, getChildren: x => x.children })),
    }
  }, [sideMenuCustomizer])

  return (
    <PanelGroup
      direction='horizontal'
      autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_X"
      className={darkMode ? 'dark' : undefined}
      style={{ fontFamily: fontFamily ?? Util.DEFAULT_FONT_FAMILY }}>

      {/* サイドメニュー */}
      <Panel defaultSize={20} className={collapsed ? 'hidden' : ''}>
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

      <PanelResizeHandle className={`w-1 bg-color-base ${collapsed ? 'hidden' : ''}`} />

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
  )
}

const ApplicationRootOutOfContext = ({ customizer }: {
  customizer?: AutoGeneratedCustomizer
}) => {
  // 自動生成された後のソースのカスタマイズ設定
  const customizerContextValue = useMemo(() => {
    return customizer ?? createNewCustomizerContext()
  }, [customizer])

  return (
    <QueryClientProvider client={queryClient}>
      <Util.MsgContextProvider>
        <Util.ToastContextProvider>
          <Util.LocalRepositoryContextProvider>
            <Util.UserSettingContextProvider>
              <DialogContextProvider>
                <Util.SideMenuContextProvider>
                  <CustomizerContextProvider value={customizerContextValue}>
                    {customizer?.LoginPage ? (
                      // ログイン画面がある場合はログイン画面を通す。
                      // アプリケーション本体を表示するかどうかはログイン画面の制御に任せる。
                      <customizer.LoginPage LoggedInContents={<ApplicationRootInContext />} />
                    ) : (
                      // ログイン画面が無い場合はコンテンツをそのまま表示
                      <ApplicationRootInContext />
                    )}
                  </CustomizerContextProvider>
                  <Util.EnvNameRibbon />
                  <Util.Toast />
                </Util.SideMenuContextProvider>
              </DialogContextProvider>
            </Util.UserSettingContextProvider>
          </Util.LocalRepositoryContextProvider>
        </Util.ToastContextProvider>
      </Util.MsgContextProvider>
    </QueryClientProvider>
  )
}

export function DefaultNijoApp({ customizer }: {
  customizer?: AutoGeneratedCustomizer
}) {
  // ルーティング設定
  const modifyRoutes = customizer?.useRouteCustomizer?.()
  const router = useMemo(() => {
    const defaultRoutes: RouteObject[] = [
      { path: '/', element: <DashBoard /> },
      { path: '/settings', element: <Util.ServerSettingScreen /> },
      { path: '*', element: <p> Not found.</p> },
      ...AutoGenerated.routes,
    ]
    if (AutoGenerated.SHOW_LOCAL_REPOSITORY_MENU) {
      defaultRoutes.push({ path: '/changes', element: <Util.LocalReposChangeListPage /> })
    }

    return createBrowserRouter([{
      path: '/',
      element: <ApplicationRootOutOfContext customizer={customizer} />,
      children: modifyRoutes?.(defaultRoutes) ?? defaultRoutes,
    },
    ])
  }, [modifyRoutes, customizer])

  return (
    <RouterProvider router={router} />
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
