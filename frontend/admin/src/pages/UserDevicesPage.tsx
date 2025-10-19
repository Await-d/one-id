import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  message,
  Popconfirm,
  Modal,
  Form,
  Input,
  Statistic,
  Row,
  Col,
  Descriptions,
} from "antd";
import {
  DeleteOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  EditOutlined,
  MobileOutlined,
  DesktopOutlined,
  TabletOutlined,
  GlobalOutlined,
  SafetyOutlined,
  StopOutlined,
} from "@ant-design/icons";
import { userDevicesApi, UserDevice, DeviceStatistics } from "../lib/userDevicesApi";
import type { ColumnsType } from "antd/es/table";

export default function UserDevicesPage() {
  const { t } = useTranslation();
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const [devices, setDevices] = useState<UserDevice[]>([]);
  const [statistics, setStatistics] = useState<DeviceStatistics | null>(null);
  const [loading, setLoading] = useState(false);
  const [renameModalVisible, setRenameModalVisible] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<UserDevice | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    if (userId) {
      loadDevices();
      loadStatistics();
    }
  }, [userId]);

  const loadDevices = async () => {
    if (!userId) return;

    setLoading(true);
    try {
      const data = await userDevicesApi.getUserDevices(userId);
      setDevices(data);
    } catch (error) {
      message.error(t('userDevices.loadDevicesFailed'));
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async () => {
    if (!userId) return;

    try {
      const stats = await userDevicesApi.getDeviceStatistics(userId);
      setStatistics(stats);
    } catch (error) {
      console.error("Failed to load statistics:", error);
    }
  };

  const handleTrustDevice = async (deviceId: string, trusted: boolean) => {
    try {
      await userDevicesApi.trustDevice(deviceId, trusted);
      message.success(trusted ? t('userDevices.trustSuccess') : t('userDevices.untrustSuccess'));
      loadDevices();
      loadStatistics();
    } catch (error) {
      message.error(t('userDevices.operationFailed'));
      console.error(error);
    }
  };

  const handleSetActive = async (deviceId: string, active: boolean) => {
    try {
      await userDevicesApi.setDeviceActive(deviceId, active);
      message.success(active ? t('userDevices.activateSuccess') : t('userDevices.deactivateSuccess'));
      loadDevices();
      loadStatistics();
    } catch (error) {
      message.error(t('userDevices.operationFailed'));
      console.error(error);
    }
  };

  const handleDeleteDevice = async (deviceId: string) => {
    try {
      await userDevicesApi.deleteDevice(deviceId);
      message.success(t('userDevices.deleteSuccess'));
      loadDevices();
      loadStatistics();
    } catch (error) {
      message.error(t('userDevices.deleteFailed'));
      console.error(error);
    }
  };

  const showRenameModal = (device: UserDevice) => {
    setSelectedDevice(device);
    form.setFieldsValue({ deviceName: device.deviceName });
    setRenameModalVisible(true);
  };

  const handleRenameDevice = async () => {
    if (!selectedDevice) return;

    try {
      const values = await form.validateFields();
      await userDevicesApi.renameDevice(selectedDevice.id, values.deviceName);
      message.success(t('userDevices.renameSuccess'));
      setRenameModalVisible(false);
      loadDevices();
    } catch (error) {
      message.error(t('userDevices.renameFailed'));
      console.error(error);
    }
  };

  const getDeviceIcon = (deviceType?: string) => {
    switch (deviceType?.toLowerCase()) {
      case "mobile":
        return <MobileOutlined />;
      case "tablet":
        return <TabletOutlined />;
      case "desktop":
      default:
        return <DesktopOutlined />;
    }
  };

  const columns: ColumnsType<UserDevice> = [
    {
      title: t('userDevices.deviceName'),
      dataIndex: "deviceName",
      key: "deviceName",
      render: (name, record) => (
        <Space>
          {getDeviceIcon(record.deviceType)}
          <span>{name || t('userDevices.unnamed')}</span>
        </Space>
      ),
    },
    {
      title: t('userDevices.browser'),
      dataIndex: "browser",
      key: "browser",
      render: (browser, record) =>
        browser ? `${browser} ${record.browserVersion || ""}` : "-",
    },
    {
      title: t('userDevices.operatingSystem'),
      dataIndex: "operatingSystem",
      key: "operatingSystem",
      render: (os, record) => (os ? `${os} ${record.osVersion || ""}` : "-"),
    },
    {
      title: t('userDevices.lastUsed'),
      dataIndex: "lastUsedAt",
      key: "lastUsedAt",
      render: (date) => new Date(date).toLocaleString("zh-CN"),
      sorter: (a, b) => new Date(b.lastUsedAt).getTime() - new Date(a.lastUsedAt).getTime(),
    },
    {
      title: t('userDevices.usageCount'),
      dataIndex: "usageCount",
      key: "usageCount",
      sorter: (a, b) => b.usageCount - a.usageCount,
    },
    {
      title: t('userDevices.status'),
      key: "status",
      render: (_, record) => (
        <Space direction="vertical" size="small">
          {record.isTrusted && (
            <Tag icon={<SafetyOutlined />} color="success">
              {t('userDevices.trusted')}
            </Tag>
          )}
          {record.isActive ? (
            <Tag icon={<CheckCircleOutlined />} color="green">
              {t('userDevices.active')}
            </Tag>
          ) : (
            <Tag icon={<StopOutlined />} color="default">
              {t('userDevices.inactive')}
            </Tag>
          )}
        </Space>
      ),
    },
    {
      title: t('userDevices.actions'),
      key: "action",
      render: (_, record) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => showRenameModal(record)}
          >
            {t('userDevices.rename')}
          </Button>
          <Button
            type="link"
            size="small"
            onClick={() => handleTrustDevice(record.id, !record.isTrusted)}
          >
            {record.isTrusted ? t('userDevices.untrust') : t('userDevices.trust')}
          </Button>
          <Button
            type="link"
            size="small"
            onClick={() => handleSetActive(record.id, !record.isActive)}
          >
            {record.isActive ? t('userDevices.deactivate') : t('userDevices.activate')}
          </Button>
          <Popconfirm
            title={t('userDevices.confirmDelete')}
            onConfirm={() => handleDeleteDevice(record.id)}
            okText={t('userDevices.confirmButton')}
            cancelText={t('userDevices.cancelButton')}
          >
            <Button type="link" danger size="small" icon={<DeleteOutlined />}>
              {t('userDevices.delete')}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: "24px" }}>
      <Space direction="vertical" size="large" style={{ width: "100%" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h2>{t('userDevices.title')}</h2>
          <Button onClick={() => navigate(-1)}>{t('userDevices.back')}</Button>
        </div>

        {statistics && (
          <Row gutter={16}>
            <Col span={6}>
              <Card>
                <Statistic
                  title={t('userDevices.totalDevices')}
                  value={statistics.totalDevices}
                  prefix={<GlobalOutlined />}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title={t('userDevices.trustedDevices')}
                  value={statistics.trustedDevices}
                  prefix={<SafetyOutlined />}
                  valueStyle={{ color: "#52c41a" }}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title={t('userDevices.activeDevices')}
                  value={statistics.activeDevices}
                  prefix={<CheckCircleOutlined />}
                  valueStyle={{ color: "#1890ff" }}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title={t('userDevices.lastAdded')}
                  value={
                    statistics.lastDeviceAdded
                      ? new Date(statistics.lastDeviceAdded).toLocaleDateString("zh-CN")
                      : "-"
                  }
                />
              </Card>
            </Col>
          </Row>
        )}

        <Card title={t('userDevices.deviceList')}>
          <Table
            columns={columns}
            dataSource={devices}
            rowKey="id"
            loading={loading}
            expandable={{
              expandedRowRender: (record) => (
                <Descriptions bordered size="small" column={2}>
                  <Descriptions.Item label={t('userDevices.deviceFingerprint')}>
                    {record.deviceFingerprint}
                  </Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.screenResolution')}>
                    {record.screenResolution || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.timezone')}>{record.timeZone || "-"}</Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.language')}>{record.language || "-"}</Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.platform')}>{record.platform || "-"}</Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.lastIp')}>{record.lastIpAddress || "-"}</Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.lastLocation')} span={2}>
                    {record.lastLocation || "-"}
                  </Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.firstUsed')}>
                    {new Date(record.firstUsedAt).toLocaleString("zh-CN")}
                  </Descriptions.Item>
                  <Descriptions.Item label={t('userDevices.lastUsed')}>
                    {new Date(record.lastUsedAt).toLocaleString("zh-CN")}
                  </Descriptions.Item>
                </Descriptions>
              ),
            }}
          />
        </Card>
      </Space>

      <Modal
        title={t('userDevices.renameDevice')}
        open={renameModalVisible}
        onOk={handleRenameDevice}
        onCancel={() => setRenameModalVisible(false)}
        okText={t('userDevices.saveButton')}
        cancelText={t('userDevices.cancelButton')}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="deviceName"
            label={t('userDevices.deviceNameLabel')}
            rules={[{ required: true, message: t('userDevices.deviceNameRequired') }]}
          >
            <Input placeholder={t('userDevices.deviceNamePlaceholder')} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

