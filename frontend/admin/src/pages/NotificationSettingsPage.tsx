import { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import {
  Card,
  Switch,
  Space,
  Typography,
  Divider,
  Button,
  message,
  Spin,
  InputNumber,
  Row,
  Col,
  Alert,
  Descriptions,
  Tag,
} from "antd";
import {
  BellOutlined,
  SafetyOutlined,
  MobileOutlined,
  LockOutlined,
  KeyOutlined,
  SaveOutlined,
  ReloadOutlined,
} from "@ant-design/icons";
import { notificationSettingsApi, type NotificationSettings } from "../lib/notificationSettingsApi";

const { Title, Text, Paragraph } = Typography;

export default function NotificationSettingsPage() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState<NotificationSettings | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  // 加载设置
  const loadSettings = async () => {
    setLoading(true);
    try {
      const data = await notificationSettingsApi.getSettings();
      setSettings(data);
      setHasChanges(false);
    } catch (error: any) {
      message.error(error.message || t('notificationSettings.loadFailed'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSettings();
  }, []);

  // 保存所有设置
  const handleSaveAll = async () => {
    if (!settings) return;

    setSaving(true);
    try {
      await notificationSettingsApi.updateAllSettings(settings);
      message.success(t('notificationSettings.saveSuccess'));
      setHasChanges(false);
    } catch (error: any) {
      message.error(error.message || t('notificationSettings.saveFailed'));
    } finally {
      setSaving(false);
    }
  };

  // 更新设置
  const updateSetting = (path: string[], value: any) => {
    if (!settings) return;

    setSettings((prev) => {
      if (!prev) return prev;
      const newSettings = { ...prev };
      let current: any = newSettings;

      for (let i = 0; i < path.length - 1; i++) {
        current = current[path[i]];
      }
      current[path[path.length - 1]] = value;

      return newSettings;
    });
    setHasChanges(true);
  };

  if (loading || !settings) {
    return (
      <div style={{ textAlign: "center", padding: "50px" }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: "24px" }}>
      <Space direction="vertical" size="large" style={{ width: "100%" }}>
        {/* 页面标题 */}
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <div>
            <Title level={2}>
              <BellOutlined /> {t('notificationSettings.title')}
            </Title>
            <Paragraph type="secondary">{t('notificationSettings.subtitle')}</Paragraph>
          </div>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadSettings} loading={loading}>
              {t('notificationSettings.refresh')}
            </Button>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              onClick={handleSaveAll}
              loading={saving}
              disabled={!hasChanges}
            >
              {t('notificationSettings.saveAll')}
            </Button>
          </Space>
        </div>

        {/* 提示信息 */}
        {hasChanges && (
          <Alert
            message={t('notificationSettings.unsavedChanges')}
            description={t('notificationSettings.unsavedChangesDesc')}
            type="warning"
            showIcon
            closable
          />
        )}

        {/* 异常登录通知 */}
        <Card
          title={
            <Space>
              <SafetyOutlined style={{ color: "#ff4d4f" }} />
              <span>{t('notificationSettings.anomalousLogin')}</span>
              <Tag color={settings.anomalousLogin.enabled ? "success" : "default"}>
                {settings.anomalousLogin.enabled ? t('notificationSettings.anomalousLoginEnabled') : t('notificationSettings.anomalousLoginDisabled')}
              </Tag>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Descriptions column={1} bordered>
                <Descriptions.Item label={t('notificationSettings.enabledStatus')}>
                  <Switch
                    checked={settings.anomalousLogin.enabled}
                    onChange={(checked) => updateSetting(["anomalousLogin", "enabled"], checked)}
                    checkedChildren={t('notificationSettings.enabledChecked')}
                    unCheckedChildren={t('notificationSettings.disabledUnchecked')}
                  />
                </Descriptions.Item>
                <Descriptions.Item label={t('notificationSettings.riskScoreThreshold')}>
                  <Space>
                    <InputNumber
                      min={0}
                      max={100}
                      value={settings.anomalousLogin.riskScoreThreshold}
                      onChange={(value) =>
                        updateSetting(["anomalousLogin", "riskScoreThreshold"], value || 40)
                      }
                      addonAfter={t('notificationSettings.score')}
                      style={{ width: 150 }}
                    />
                    <Text type="secondary">{t('notificationSettings.riskScoreHelp')}</Text>
                  </Space>
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={24}>
              <Alert
                message={t('notificationSettings.featureDescription')}
                description={
                  <div>
                    <p>
                      {t('notificationSettings.anomalousLoginDesc')}
                    </p>
                    <p style={{ marginBottom: 0 }}>
                      <strong>{t('notificationSettings.anomalousLoginRules')}</strong>
                    </p>
                  </div>
                }
                type="info"
                showIcon
              />
            </Col>
          </Row>
        </Card>

        {/* 新设备登录通知 */}
        <Card
          title={
            <Space>
              <MobileOutlined style={{ color: "#1890ff" }} />
              <span>{t('notificationSettings.newDevice')}</span>
              <Tag color={settings.newDevice.enabled ? "success" : "default"}>
                {settings.newDevice.enabled ? t('notificationSettings.anomalousLoginEnabled') : t('notificationSettings.anomalousLoginDisabled')}
              </Tag>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Descriptions column={1} bordered>
                <Descriptions.Item label={t('notificationSettings.enabledStatus')}>
                  <Switch
                    checked={settings.newDevice.enabled}
                    onChange={(checked) => updateSetting(["newDevice", "enabled"], checked)}
                    checkedChildren={t('notificationSettings.enabledChecked')}
                    unCheckedChildren={t('notificationSettings.disabledUnchecked')}
                  />
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={24}>
              <Alert
                message={t('notificationSettings.featureDescription')}
                description={t('notificationSettings.newDeviceDesc')}
                type="info"
                showIcon
              />
            </Col>
          </Row>
        </Card>

        {/* 密码修改通知 */}
        <Card
          title={
            <Space>
              <LockOutlined style={{ color: "#52c41a" }} />
              <span>{t('notificationSettings.passwordChanged')}</span>
              <Tag color={settings.passwordChanged.enabled ? "success" : "default"}>
                {settings.passwordChanged.enabled ? t('notificationSettings.anomalousLoginEnabled') : t('notificationSettings.anomalousLoginDisabled')}
              </Tag>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Descriptions column={1} bordered>
                <Descriptions.Item label={t('notificationSettings.enabledStatus')}>
                  <Switch
                    checked={settings.passwordChanged.enabled}
                    onChange={(checked) => updateSetting(["passwordChanged", "enabled"], checked)}
                    checkedChildren={t('notificationSettings.enabledChecked')}
                    unCheckedChildren={t('notificationSettings.disabledUnchecked')}
                  />
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={24}>
              <Alert
                message={t('notificationSettings.featureDescription')}
                description={t('notificationSettings.passwordChangedDesc')}
                type="info"
                showIcon
              />
            </Col>
          </Row>
        </Card>

        {/* 账户锁定通知 */}
        <Card
          title={
            <Space>
              <KeyOutlined style={{ color: "#faad14" }} />
              <span>{t('notificationSettings.accountLocked')}</span>
              <Tag color={settings.accountLocked.enabled ? "success" : "default"}>
                {settings.accountLocked.enabled ? t('notificationSettings.anomalousLoginEnabled') : t('notificationSettings.anomalousLoginDisabled')}
              </Tag>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Descriptions column={1} bordered>
                <Descriptions.Item label={t('notificationSettings.enabledStatus')}>
                  <Switch
                    checked={settings.accountLocked.enabled}
                    onChange={(checked) => updateSetting(["accountLocked", "enabled"], checked)}
                    checkedChildren={t('notificationSettings.enabledChecked')}
                    unCheckedChildren={t('notificationSettings.disabledUnchecked')}
                  />
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={24}>
              <Alert
                message={t('notificationSettings.featureDescription')}
                description={t('notificationSettings.accountLockedDesc')}
                type="info"
                showIcon
              />
            </Col>
          </Row>
        </Card>

        {/* MFA启用通知 */}
        <Card
          title={
            <Space>
              <SafetyOutlined style={{ color: "#722ed1" }} />
              <span>{t('notificationSettings.mfaEnabled')}</span>
              <Tag color={settings.mfaEnabled.enabled ? "success" : "default"}>
                {settings.mfaEnabled.enabled ? t('notificationSettings.anomalousLoginEnabled') : t('notificationSettings.anomalousLoginDisabled')}
              </Tag>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Descriptions column={1} bordered>
                <Descriptions.Item label={t('notificationSettings.enabledStatus')}>
                  <Switch
                    checked={settings.mfaEnabled.enabled}
                    onChange={(checked) => updateSetting(["mfaEnabled", "enabled"], checked)}
                    checkedChildren={t('notificationSettings.enabledChecked')}
                    unCheckedChildren={t('notificationSettings.disabledUnchecked')}
                  />
                </Descriptions.Item>
              </Descriptions>
            </Col>
            <Col span={24}>
              <Alert
                message={t('notificationSettings.featureDescription')}
                description={t('notificationSettings.mfaEnabledDesc')}
                type="info"
                showIcon
              />
            </Col>
          </Row>
        </Card>

        {/* 底部保存按钮 */}
        <div style={{ textAlign: "right" }}>
          <Space>
            <Button onClick={loadSettings} disabled={!hasChanges}>
              {t('notificationSettings.cancelChanges')}
            </Button>
            <Button
              type="primary"
              size="large"
              icon={<SaveOutlined />}
              onClick={handleSaveAll}
              loading={saving}
              disabled={!hasChanges}
            >
              {t('notificationSettings.saveAll')}
            </Button>
          </Space>
        </div>
      </Space>
    </div>
  );
}

