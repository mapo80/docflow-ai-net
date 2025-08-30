import { Layout, Menu, Button, Drawer, Grid } from 'antd';
import {
  useNavigate,
  Outlet,
  useLocation,
} from 'react-router-dom';
import AppstoreOutlined from '@ant-design/icons/AppstoreOutlined';
import FileAddOutlined from '@ant-design/icons/FileAddOutlined';
import HeartOutlined from '@ant-design/icons/HeartOutlined';
import LinkOutlined from '@ant-design/icons/LinkOutlined';
import ExperimentOutlined from '@ant-design/icons/ExperimentOutlined';
import MenuOutlined from '@ant-design/icons/MenuOutlined';
import RobotOutlined from '@ant-design/icons/RobotOutlined';
import FileMarkdownOutlined from '@ant-design/icons/FileMarkdownOutlined';
import SettingOutlined from '@ant-design/icons/SettingOutlined';
import BranchesOutlined from '@ant-design/icons/BranchesOutlined';
import TagsOutlined from '@ant-design/icons/TagsOutlined';
import ToolOutlined from '@ant-design/icons/ToolOutlined';
import { useState } from 'react';
import { openHangfire } from './hangfire';

const { Sider, Header, Content } = Layout;
const { useBreakpoint } = Grid;

export default function Shell() {
  const navigate = useNavigate();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState<boolean>(
    () => localStorage.getItem('menuCollapsed') === 'true',
  );
  const screens = useBreakpoint();
  const isMobile = screens.md === false;

  const handleCollapse = (value: boolean, type: 'clickTrigger' | 'responsive') => {
    if (type === 'clickTrigger') {
      localStorage.setItem('menuCollapsed', String(value));
      setCollapsed(value);
    }
  };

  const items = [
    { key: '/jobs', icon: <AppstoreOutlined />, label: 'Jobs' },
    { key: '/jobs/new', icon: <FileAddOutlined />, label: 'New Job' },
    { key: '/markdown', icon: <FileMarkdownOutlined />, label: 'Markdown' },
    { key: '/models', icon: <ExperimentOutlined />, label: 'Models' },
    {
      key: 'rules',
      icon: <BranchesOutlined />,
      label: 'Rules Manager',
      children: [
        { key: '/rules', icon: <BranchesOutlined />, label: 'Rules' },
        { key: '/rules/tags', icon: <TagsOutlined />, label: 'Tags' },
        { key: '/rules/builder', icon: <ToolOutlined />, label: 'Builder' },
      ],
    },
    { key: '/templates', icon: <AppstoreOutlined />, label: 'Templates' },
    { key: '/markdown-systems', icon: <SettingOutlined />, label: 'Markdown Systems' },
    { key: '/health', icon: <HeartOutlined />, label: 'Health' },
    {
      key: 'hangfire',
      icon: <LinkOutlined />,
      label: 'Hangfire',
      onClick: openHangfire,
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {!isMobile && (
        <Sider
          collapsible
          collapsed={collapsed}
          onCollapse={handleCollapse}
          collapsedWidth={0}
          trigger={null}
        >
          <Menu
            selectedKeys={[location.pathname]}
            mode="inline"
            items={items}
            onClick={(e) => {
              if (e.key === 'hangfire') return;
              navigate(e.key);
            }}
            style={{ height: '100%' }}
          />
        </Sider>
      )}
      <Layout>
        <Header
          style={{
            padding: '0 16px',
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            gap: 12,
          }}
        >
          <Button
            type="text"
            aria-label="toggle-menu"
            icon={<MenuOutlined />}
            onClick={() => handleCollapse(!collapsed, 'clickTrigger')}
          />
          <RobotOutlined style={{ fontSize: 24 }} />
          <span style={{ fontWeight: 600 }}>DocFlow AI</span>
        </Header>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
      {isMobile && (
        <Drawer
          placement="left"
          closable={false}
          onClose={() => handleCollapse(true, 'clickTrigger')}
          open={!collapsed}
          styles={{ body: { padding: 0 } }}
          width={200}
        >
          <Menu
            selectedKeys={[location.pathname]}
            mode="inline"
            items={items}
            onClick={(e) => {
              if (e.key === 'hangfire') return;
              navigate(e.key);
              handleCollapse(true, 'clickTrigger');
            }}
            style={{ height: '100%' }}
          />
        </Drawer>
      )}
    </Layout>
  );
}
