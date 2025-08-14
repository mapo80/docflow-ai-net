import { Layout, Menu, Input } from 'antd';
import { useNavigate, Outlet, useLocation } from 'react-router-dom';
import { useState } from 'react';
import {
  AppstoreOutlined,
  PlusOutlined,
  SettingOutlined,
  HeartOutlined,
  LinkOutlined,
} from '@ant-design/icons';
import HealthBadge from './components/HealthBadge';

const { Header, Content, Sider } = Layout;

export default function Shell() {
  const navigate = useNavigate();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(false);
  const items = [
    { key: '/jobs', icon: <AppstoreOutlined />, label: 'Jobs' },
    { key: '/jobs/new', icon: <PlusOutlined />, label: 'Nuovo Job' },
    { key: '/model', icon: <AppstoreOutlined />, label: 'Modello' },
    { key: '/health', icon: <HeartOutlined />, label: 'Health' },
    { key: '/settings', icon: <SettingOutlined />, label: 'Settings' },
    {
      key: 'hangfire',
      icon: <LinkOutlined />,
      label: 'Hangfire',
      onClick: () =>
        window.open(
          `${import.meta.env.VITE_API_BASE_URL}${import.meta.env.VITE_HANGFIRE_PATH}`,
          '_blank',
          'noopener,noreferrer',
        ),
    },
  ];
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <Menu
          selectedKeys={[location.pathname]}
          mode="inline"
          items={items}
          onClick={(e) => {
            if (e.key === 'hangfire') return;
            navigate(e.key);
          }}
        />
      </Sider>
      <Layout>
        <Header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <HealthBadge />
          <Input.Search
            placeholder="Job ID"
            onSearch={(value) => value && navigate(`/jobs/${value}`)}
            style={{ width: 200 }}
            allowClear
          />
        </Header>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
