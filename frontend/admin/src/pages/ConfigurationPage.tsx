import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Card,
  Button,
  Space,
  Typography,
  Statistic,
  Row,
  Col,
  Alert,
  Modal,
  message,
  Descriptions,
  Tag,
  Spin,
  Divider,
} from 'antd';
import {
  ReloadOutlined,
  PoweroffOutlined,
  CloudServerOutlined,
  SafetyCertificateOutlined,
  ApiOutlined,
  ClockCircleOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import {
  getConfigurationStatus,
  getAppInfo,
  reloadAllConfigurations,
  reloadRateLimitConfiguration,
  reloadCorsConfiguration,
  reloadExternalAuthConfiguration,
  restartApplication,
  ConfigurationStatus,
  AppInfo,
} from '../lib/configurationApi';

const { Title, Text } = Typography;

const ConfigurationPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [restartModalVisible, setRestartModalVisible] = useState(false);

  // 获取配置状态
  const { data: configStatus, isLoading: statusLoading } = useQuery({
    queryKey: ['configurationStatus'],
    queryFn: getConfigurationStatus,
    refetchInterval: 30000, // 30秒刷新一次
  });

  // 获取应用信息
  const { data: appInfo, isLoading: appInfoLoading } = useQuery({
    queryKey: ['appInfo'],
    queryFn: getAppInfo,
  });

  // 刷新所有配置
  const reloadAllMutation = useMutation({
    mutationFn: reloadAllConfigurations,
    onSuccess: () => {
      message.success(t('configuration.reloadAllSuccess'));
      queryClient.invalidateQueries({ queryKey: ['configurationStatus'] });
    },
    onError: () => {
      message.error(t('configuration.reloadFailed'));
    },
  });

  // 刷新速率限制
  const reloadRateLimitMutation = useMutation({
    mutationFn: reloadRateLimitConfiguration,
    onSuccess: () => {
      message.success(t('configuration.reloadRateLimitSuccess'));
      queryClient.invalidateQueries({ queryKey: ['configurationStatus'] });
    },
    onError: () => {
      message.error(t('configuration.reloadFailed'));
    },
  });

  // 刷新 CORS
  const reloadCorsMutation = useMutation({
    mutationFn: reloadCorsConfiguration,
    onSuccess: () => {
      message.success(t('configuration.reloadCorsSuccess'));
      queryClient.invalidateQueries({ queryKey: ['configurationStatus'] });
    },
    onError: () => {
      message.error(t('configuration.reloadFailed'));
    },
  });

  // 刷新外部认证
  const reloadExternalAuthMutation = useMutation({
    mutationFn: reloadExternalAuthConfiguration,
    onSuccess: () => {
      message.success(t('configuration.reloadExternalAuthSuccess'));
      queryClient.invalidateQueries({ queryKey: ['configurationStatus'] });
    },
    onError: () => {
      message.error(t('configuration.reloadFailed'));
    },
  });

  // 重启应用
  const restartMutation = useMutation({
    mutationFn: () => restartApplication(true),
    onSuccess: () => {
      message.warning(t('configuration.restartInitiated'));
      setRestartModalVisible(false);
    },
    onError: () => {
      message.error(t('configuration.restartFailed'));
    },
  });

  const formatDate = (dateStr: string) => {
    if (!dateStr || dateStr === '0001-01-01T00:00:00') return '-';
    return new Date(dateStr).toLocaleString();
  };

  const formatUptime = (uptime: string) => {
    if (!uptime) return '-';
    // 格式: "1.02:30:45.1234567"
    const match = uptime.match(/(\d+)?\.?(\d{2}):(\d{2}):(\d{2})/);
    if (!match) return uptime;
    const [, days, hours, minutes, seconds] = match;
    const parts = [];
    if (days && parseInt(days) > 0) parts.push(`${days}d`);
    if (hours) parts.push(`${hours}h`);
    if (minutes) parts.push(`${minutes}m`);
    if (seconds) parts.push(`${seconds}s`);
    return parts.join(' ');
  };

  return (
    <div style={{ padding: 24 }}>
      <Title level={2}>{t('configuration.title')}</Title>
      <Text type="secondary">{t('configuration.description')}</Text>

      <Alert
        message={t('configuration.restartNotice')}
        description={t('configuration.restartNoticeDesc')}
        type="info"
        showIcon
        style={{ marginTop: 16, marginBottom: 24 }}
      />

      {/* 应用信息 */}
      <Card
        title={
          <Space>
            <CloudServerOutlined />
            {t('configuration.appInfo')}
          </Space>
        }
        style={{ marginBottom: 24 }}
        extra={
          <Button
            danger
            icon={<PoweroffOutlined />}
            onClick={() => setRestartModalVisible(true)}
          >
            {t('configuration.restart')}
          </Button>
        }
      >
        {appInfoLoading ? (
          <Spin />
        ) : appInfo ? (
          <Descriptions column={3}>
            <Descriptions.Item label={t('configuration.environment')}>
              <Tag color={appInfo.environment === 'Development' ? 'blue' : 'green'}>
                {appInfo.environment}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label={t('configuration.machineName')}>
              {appInfo.machineName}
            </Descriptions.Item>
            <Descriptions.Item label={t('configuration.processId')}>
              {appInfo.processId}
            </Descriptions.Item>
            <Descriptions.Item label={t('configuration.startTime')}>
              {formatDate(appInfo.startTime)}
            </Descriptions.Item>
            <Descriptions.Item label={t('configuration.uptime')}>
              <Tag icon={<ClockCircleOutlined />} color="processing">
                {formatUptime(appInfo.uptime)}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label={t('configuration.dotNetVersion')}>
              {appInfo.dotNetVersion}
            </Descriptions.Item>
          </Descriptions>
        ) : null}
      </Card>

      {/* 配置状态 */}
      <Card
        title={
          <Space>
            <SafetyCertificateOutlined />
            {t('configuration.configStatus')}
          </Space>
        }
        extra={
          <Button
            type="primary"
            icon={<ReloadOutlined />}
            loading={reloadAllMutation.isPending}
            onClick={() => reloadAllMutation.mutate()}
          >
            {t('configuration.reloadAll')}
          </Button>
        }
      >
        {statusLoading ? (
          <Spin />
        ) : configStatus ? (
          <Row gutter={[24, 24]}>
            {/* 速率限制配置 */}
            <Col xs={24} md={8}>
              <Card
                size="small"
                title={
                  <Space>
                    {t('configuration.rateLimit')}
                    <Tag color="warning">{t('configuration.requiresRestart')}</Tag>
                  </Space>
                }
                extra={
                  <Button
                    type="dashed"
                    size="small"
                    icon={<ReloadOutlined />}
                    loading={reloadRateLimitMutation.isPending}
                    onClick={() => reloadRateLimitMutation.mutate()}
                  >
                    {t('configuration.reload')}
                  </Button>
                }
              >
                <Statistic
                  title={t('configuration.settingsCount')}
                  value={configStatus.rateLimit.settingsCount}
                  suffix={`/ ${configStatus.rateLimit.enabledCount} ${t('configuration.enabled')}`}
                />
                <Divider style={{ margin: '12px 0' }} />
                <Text type="secondary">
                  {t('configuration.version')}: {configStatus.rateLimit.version}
                </Text>
                <br />
                <Text type="secondary">
                  {t('configuration.lastUpdated')}: {formatDate(configStatus.rateLimit.lastUpdated)}
                </Text>
              </Card>
            </Col>

            {/* CORS 配置 */}
            <Col xs={24} md={8}>
              <Card
                size="small"
                title={
                  <Space>
                    {t('configuration.cors')}
                    <Tag color="warning">{t('configuration.requiresRestart')}</Tag>
                  </Space>
                }
                extra={
                  <Button
                    type="dashed"
                    size="small"
                    icon={<ReloadOutlined />}
                    loading={reloadCorsMutation.isPending}
                    onClick={() => reloadCorsMutation.mutate()}
                  >
                    {t('configuration.reload')}
                  </Button>
                }
              >
                <Statistic
                  title={t('configuration.originsCount')}
                  value={configStatus.cors.originsCount}
                  suffix={
                    configStatus.cors.allowAnyOrigin ? (
                      <Tag color="warning">{t('configuration.allowAny')}</Tag>
                    ) : null
                  }
                />
                <Divider style={{ margin: '12px 0' }} />
                <Text type="secondary">
                  {t('configuration.version')}: {configStatus.cors.version}
                </Text>
                <br />
                <Text type="secondary">
                  {t('configuration.lastUpdated')}: {formatDate(configStatus.cors.lastUpdated)}
                </Text>
              </Card>
            </Col>

            {/* 外部认证配置 */}
            <Col xs={24} md={8}>
              <Card
                size="small"
                title={
                  <Space>
                    {t('configuration.externalAuth')}
                    <Tag color="warning">{t('configuration.requiresRestart')}</Tag>
                  </Space>
                }
                extra={
                  <Button
                    type="dashed"
                    size="small"
                    icon={<ReloadOutlined />}
                    loading={reloadExternalAuthMutation.isPending}
                    onClick={() => reloadExternalAuthMutation.mutate()}
                  >
                    {t('configuration.reload')}
                  </Button>
                }
              >
                <Statistic
                  title={t('configuration.providersCount')}
                  value={configStatus.externalAuth.providersCount}
                  prefix={<ApiOutlined />}
                />
                <Divider style={{ margin: '12px 0' }} />
                <Text type="secondary">
                  {t('configuration.version')}: {configStatus.externalAuth.version}
                </Text>
                <br />
                <Text type="secondary">
                  {t('configuration.lastUpdated')}: {formatDate(configStatus.externalAuth.lastUpdated)}
                </Text>
              </Card>
            </Col>
          </Row>
        ) : null}
      </Card>

      {/* 重启确认弹窗 */}
      <Modal
        title={
          <Space>
            <ExclamationCircleOutlined style={{ color: '#faad14' }} />
            {t('configuration.restartConfirmTitle')}
          </Space>
        }
        open={restartModalVisible}
        onOk={() => restartMutation.mutate()}
        onCancel={() => setRestartModalVisible(false)}
        okText={t('configuration.confirmRestart')}
        cancelText={t('common.cancel')}
        okButtonProps={{ danger: true, loading: restartMutation.isPending }}
      >
        <Alert
          message={t('configuration.restartWarning')}
          description={t('configuration.restartWarningDesc')}
          type="warning"
          showIcon
        />
      </Modal>
    </div>
  );
};

export default ConfigurationPage;
