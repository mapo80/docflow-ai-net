import { Layout, Menu, Button } from 'antd';
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
import MenuFoldOutlined from '@ant-design/icons/MenuFoldOutlined';
import MenuUnfoldOutlined from '@ant-design/icons/MenuUnfoldOutlined';
import { useState } from 'react';
import { openHangfire } from './hangfire';

const { Sider, Header, Content } = Layout;

export default function Shell() {
  const navigate = useNavigate();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState<boolean>(
    () => localStorage.getItem('menuCollapsed') === 'true',
  );

  const handleCollapse = (value: boolean, type: 'clickTrigger' | 'responsive') => {
    if (type === 'clickTrigger') {
      localStorage.setItem('menuCollapsed', String(value));
      setCollapsed(value);
    }
  };

  const items = [
    { key: '/jobs', icon: <AppstoreOutlined />, label: 'Jobs' },
    { key: '/jobs/new', icon: <FileAddOutlined />, label: 'New Job' },
    { key: '/models', icon: <ExperimentOutlined />, label: 'Models' },
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
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={handleCollapse}
        breakpoint="md"
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
      <Layout>
        <Header
          style={{
            padding: 0,
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
          }}
        >
          <Button
            type="text"
            aria-label="toggle-menu"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => handleCollapse(!collapsed, 'clickTrigger')}
          />
        </Header>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
