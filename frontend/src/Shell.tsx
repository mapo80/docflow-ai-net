import { Suspense } from 'react';
import { Layout, Menu } from 'antd';
import { useNavigate, Outlet, useLocation } from 'react-router-dom';
import AppstoreOutlined from '@ant-design/icons/AppstoreOutlined';
import FileAddOutlined from '@ant-design/icons/FileAddOutlined';
import HeartOutlined from '@ant-design/icons/HeartOutlined';
import LinkOutlined from '@ant-design/icons/LinkOutlined';
import ExperimentOutlined from '@ant-design/icons/ExperimentOutlined';
import HealthBadge from './components/HealthBadge';
import ChunkLoader from './components/ChunkLoader';
import Loader from './components/Loader';
import { openHangfire } from './hangfire';

const { Header, Content } = Layout;

export default function Shell() {
  const navigate = useNavigate();
  const location = useLocation();
  const items = [
    { key: '/jobs', icon: <AppstoreOutlined />, label: 'Jobs' },
    { key: '/jobs/new', icon: <FileAddOutlined />, label: 'New Job' },
    { key: '/model', icon: <ExperimentOutlined />, label: 'Model' },
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
      <Header
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          background: '#fff',
        }}
      >
        <Menu
          selectedKeys={[location.pathname]}
          mode="horizontal"
          items={items}
          onClick={(e) => {
            if (e.key === 'hangfire') return;
            navigate(e.key);
          }}
          style={{ flex: 1 }}
          theme="light"
        />
        <HealthBadge />
      </Header>
      <Content style={{ padding: 24 }}>
        <Suspense fallback={<Loader />}><Suspense fallback={<ChunkLoader />}><Outlet /></Suspense></Suspense>
      </Content>
    </Layout>
  );
}
