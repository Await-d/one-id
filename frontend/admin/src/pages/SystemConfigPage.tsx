import { useState, useEffect, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Card,
  Tabs,
  Form,
  Input,
  Select,
  Switch,
  Button,
  Space,
  Typography,
  message,
  Spin,
  Alert,
  Divider,
} from 'antd';
import {
  SettingOutlined,
  GlobalOutlined,
  SafetyOutlined,
} from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { clientSettingsApi, type ClientValidationSettings, type UpdateClientValidationSettingsPayload } from '../lib/clientSettingsApi';
import { corsSettingsApi, type CorsSettings, type UpdateCorsSettingsPayload } from '../lib/corsSettingsApi';

const { Title, Text, Paragraph } = Typography;

const SCHEME_OPTIONS = ['https', 'http'];

interface ConfigSection {
  key: string;
  label: string;
  icon: React.ReactNode;
  color: string;
}

export default function SystemConfigPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [corsForm] = Form.useForm();
  const [clientForm] = Form.useForm();
  const [activeTab, setActiveTab] = useState<string>('cors');

  // 配置章节定义（使用 i18n，使用 useMemo 优化）
  const CONFIG_SECTIONS: ConfigSection[] = useMemo(() => [
    {
      key: 'cors',
      label: t('systemConfig.corsLabel'),
      icon: <GlobalOutlined />,
      color: '#1890ff',
    },
    {
      key: 'clientValidation',
      label: t('systemConfig.clientValidationLabel'),
      icon: <SafetyOutlined />,
      color: '#52c41a',
    },
  ], [t]);

  // CORS 设置查询
  const corsQuery = useQuery({
    queryKey: ['cors-settings'],
    queryFn: () => corsSettingsApi.get(),
  });

  // 客户端验证设置查询
  const clientQuery = useQuery({
    queryKey: ['client-validation-settings'],
    queryFn: () => clientSettingsApi.getValidation(),
  });

  // CORS 更新
  const corsMutation = useMutation({
    mutationFn: corsSettingsApi.update,
    onSuccess: async () => {
      message.success(t('cors.updateSuccess'));
      await queryClient.invalidateQueries({ queryKey: ['cors-settings'] });
    },
    onError: (error: Error & { response?: { data?: { detail?: string } } }) => {
      message.error(error?.response?.data?.detail || t('cors.updateFailed'));
    },
  });

  // 客户端验证更新
  const clientMutation = useMutation({
    mutationFn: clientSettingsApi.updateValidation,
    onSuccess: async () => {
      message.success(t('clientValidation.updateSuccess'));
      await queryClient.invalidateQueries({ queryKey: ['client-validation-settings'] });
    },
    onError: (error: Error & { response?: { data?: { detail?: string } } }) => {
      message.error(error?.response?.data?.detail || t('clientValidation.updateFailed'));
    },
  });

  // 更新 CORS 表单
  useEffect(() => {
    if (corsQuery.data) {
      corsForm.setFieldsValue({
        allowedOrigins: corsQuery.data.allowedOrigins || [],
        allowAnyOrigin: corsQuery.data.allowAnyOrigin || false,
      });
    }
  }, [corsQuery.data, corsForm]);

  // 更新客户端验证表单
  useEffect(() => {
    if (clientQuery.data) {
      clientForm.setFieldsValue({
        allowedSchemes: clientQuery.data.allowedSchemes || SCHEME_OPTIONS,
        allowHttpOnLoopback: clientQuery.data.allowHttpOnLoopback || true,
        allowedHosts: clientQuery.data.allowedHosts || [],
      });
    }
  }, [clientQuery.data, clientForm]);

  // URL 格式验证函数（使用 useCallback 避免重复创建）
  const isValidUrl = useCallback((url: string): boolean => {
    try {
      const urlObj = new URL(url);
      return ['http:', 'https:'].includes(urlObj.protocol);
    } catch {
      return false;
    }
  }, []);

  // 处理 CORS 提交（使用 useCallback 优化）
  const handleCorsSubmit = useCallback((values: UpdateCorsSettingsPayload) => {
    const allowedOrigins = (values.allowedOrigins || [])
      .map((origin: string) => origin.trim())
      .filter(Boolean);

    // 验证 URL 格式
    const invalidOrigins = allowedOrigins.filter(origin => !isValidUrl(origin));
    if (invalidOrigins.length > 0) {
      message.error(`${t('cors.invalidOrigins')}: ${invalidOrigins.join(', ')}`);
      return;
    }

    corsMutation.mutate({
      allowedOrigins,
      allowAnyOrigin: !!values.allowAnyOrigin,
    });
  }, [t, isValidUrl, corsMutation]);

  // 处理客户端验证提交（使用 useCallback 优化）
  const handleClientSubmit = useCallback((values: UpdateClientValidationSettingsPayload) => {
    const payload = {
      allowedSchemes: (values.allowedSchemes || []).map((s: string) => s.trim()).filter(Boolean),
      allowHttpOnLoopback: !!values.allowHttpOnLoopback,
      allowedHosts: (values.allowedHosts || []).map((h: string) => h.trim()).filter(Boolean),
    };

    clientMutation.mutate(payload);
  }, [clientMutation]);

  const allowAnyOrigin = Form.useWatch('allowAnyOrigin', corsForm);

  const renderCorsConfig = () => (
    <div>
      <Paragraph type="secondary">{t('cors.description')}</Paragraph>
      <Divider />
      <Form
        form={corsForm}
        layout="vertical"
        onFinish={handleCorsSubmit}
        initialValues={{ allowAnyOrigin: false, allowedOrigins: [] }}
      >
        <Form.Item
          name="allowAnyOrigin"
          label={t('cors.allowAnyOrigin')}
          valuePropName="checked"
        >
          <Switch />
        </Form.Item>

        <Form.Item
          name="allowedOrigins"
          label={t('cors.allowedOrigins')}
          tooltip={t('cors.originsTooltip')}
        >
          <Select
            mode="tags"
            tokenSeparators={[',', ' ']}
            placeholder={t('cors.originsPlaceholder')}
            disabled={allowAnyOrigin}
          />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={corsMutation.isPending}>
            {t('common.save')}
          </Button>
          <Button onClick={() => corsForm.resetFields()} disabled={corsQuery.isFetching || corsMutation.isPending}>
            {t('common.reset')}
          </Button>
        </Space>
      </Form>
    </div>
  );

  const renderClientValidationConfig = () => (
    <div>
      <Paragraph type="secondary">
        {t('systemConfig.clientValidationDescription')}
      </Paragraph>
      <Divider />
      <Form
        form={clientForm}
        layout="vertical"
        onFinish={handleClientSubmit}
        initialValues={{
          allowedSchemes: SCHEME_OPTIONS,
          allowHttpOnLoopback: true,
          allowedHosts: [],
        }}
      >
        <Form.Item
          name="allowedSchemes"
          label={t('clientValidation.allowedSchemes')}
          rules={[{ required: true, message: t('clientValidation.allowedSchemesRequired') }]}
        >
          <Select
            mode="tags"
            options={SCHEME_OPTIONS.map((v) => ({ label: v, value: v }))}
            placeholder={t('clientValidation.allowedSchemesPlaceholder')}
            tokenSeparators={[',', ' ']}
          />
        </Form.Item>

        <Form.Item
          name="allowHttpOnLoopback"
          label={t('clientValidation.allowHttpOnLoopback')}
          valuePropName="checked"
        >
          <Switch />
        </Form.Item>

        <Form.Item
          name="allowedHosts"
          label={t('clientValidation.allowedHosts')}
          tooltip={t('clientValidation.allowedHostsTooltip')}
        >
          <Select
            mode="tags"
            tokenSeparators={[',', ' ']}
            placeholder={t('clientValidation.allowedHostsPlaceholder')}
          />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={clientMutation.isPending}>
            {t('common.save')}
          </Button>
          <Button onClick={() => clientForm.resetFields()} disabled={clientQuery.isFetching || clientMutation.isPending}>
            {t('common.reset')}
          </Button>
        </Space>
      </Form>
    </div>
  );

  // 使用 useMemo 缓存计算的加载状态
  const isLoading = useMemo(() => 
    corsQuery.isLoading || clientQuery.isLoading, 
    [corsQuery.isLoading, clientQuery.isLoading]
  );
  
  const isFetching = useMemo(() => 
    corsQuery.isFetching || clientQuery.isFetching, 
    [corsQuery.isFetching, clientQuery.isFetching]
  );

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px' }}>
        <Title level={2}>
          <SettingOutlined /> {t('systemConfig.title')}
        </Title>
        <Paragraph type="secondary">
          {t('systemConfig.description')}
        </Paragraph>
      </div>

      <Alert
        message={t('systemConfig.changeNoticeTitle')}
        description={t('systemConfig.changeNoticeDescription')}
        type="info"
        showIcon
        style={{ marginBottom: '24px' }}
      />

      {isLoading && (
        <div style={{ textAlign: 'center', padding: '50px' }}>
          <Spin size="large" />
        </div>
      )}

      {!isLoading && (
        <Card>
          <Tabs
            activeKey={activeTab}
            onChange={setActiveTab}
            items={CONFIG_SECTIONS.map((section) => ({
              key: section.key,
              label: (
                <span>
                  <span style={{ color: section.color, marginRight: '8px' }}>
                    {section.icon}
                  </span>
                  {section.label}
                </span>
              ),
              children:
                section.key === 'cors'
                  ? renderCorsConfig()
                  : section.key === 'clientValidation'
                    ? renderClientValidationConfig()
                    : null,
            }))}
          />
        </Card>
      )}
    </div>
  );
}
