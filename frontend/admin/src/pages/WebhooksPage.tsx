import React, { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import {
  Table,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  Input,
  Select,
  Switch,
  message,
  Popconfirm,
  Drawer,
  Descriptions,
  Statistic,
  Row,
  Col,
  InputNumber,
  Spin,
  Typography,
  Card,
} from "antd";
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ThunderboltOutlined,
  HistoryOutlined,
  ApiOutlined,
} from "@ant-design/icons";
import { webhooksApi, Webhook, CreateWebhookDto, UpdateWebhookDto, EventType } from "../lib/webhooksApi";
import { ColumnsType } from "antd/es/table";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/zh-cn";

dayjs.extend(relativeTime);
dayjs.locale("zh-cn");

const { TextArea } = Input;
const { Title } = Typography;

export default function WebhooksPage() {
  const { t } = useTranslation();
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [eventTypes, setEventTypes] = useState<EventType[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [editingWebhook, setEditingWebhook] = useState<Webhook | null>(null);
  const [selectedWebhook, setSelectedWebhook] = useState<Webhook | null>(null);
  const [testLoading, setTestLoading] = useState<string | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadWebhooks();
    loadEventTypes();
  }, []);

  const loadWebhooks = async () => {
    setLoading(true);
    try {
      const data = await webhooksApi.getAllWebhooks();
      setWebhooks(data);
    } catch (error) {
      message.error(t('webhooks.loadFailed'));
      console.error("Failed to load webhooks:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadEventTypes = async () => {
    try {
      const data = await webhooksApi.getEventTypes();
      setEventTypes(data);
    } catch (error) {
      console.error("Failed to load event types:", error);
    }
  };

  const handleCreate = () => {
    setEditingWebhook(null);
    form.resetFields();
    form.setFieldsValue({
      isActive: true,
      maxRetries: 3,
      timeoutSeconds: 30,
      events: [],
    });
    setModalVisible(true);
  };

  const handleEdit = (webhook: Webhook) => {
    setEditingWebhook(webhook);
    form.setFieldsValue(webhook);
    setModalVisible(true);
  };

  const handleDelete = async (id: string) => {
    try {
      await webhooksApi.deleteWebhook(id);
      message.success(t('webhooks.deleteSuccess'));
      loadWebhooks();
    } catch (error) {
      message.error(t('webhooks.deleteFailed'));
      console.error("Failed to delete webhook:", error);
    }
  };

  const handleTest = async (webhook: Webhook) => {
    setTestLoading(webhook.id);
    try {
      const result = await webhooksApi.testWebhook(webhook.id);
      if (result.success) {
        Modal.success({
          title: t('webhooks.testSuccess'),
          content: (
            <div>
              <p><strong>{t('webhooks.statusCode')}:</strong> {result.statusCode}</p>
              <p><strong>{t('webhooks.responseTime')}:</strong> {result.durationMs}ms</p>
              {result.response && (
                <p><strong>{t('webhooks.response')}:</strong> <pre style={{ maxHeight: 200, overflow: "auto" }}>{result.response}</pre></p>
              )}
            </div>
          ),
        });
      } else {
        Modal.error({
          title: t('webhooks.testFailed'),
          content: (
            <div>
              <p><strong>{t('webhooks.statusCode')}:</strong> {result.statusCode}</p>
              <p><strong>{t('webhooks.error')}:</strong> {result.errorMessage}</p>
              {result.response && (
                <p><strong>{t('webhooks.response')}:</strong> <pre style={{ maxHeight: 200, overflow: "auto" }}>{result.response}</pre></p>
              )}
            </div>
          ),
        });
      }
    } catch (error: any) {
      message.error(t('webhooks.testFailed', { error: error.message || t('webhooks.unknownError') }));
    } finally {
      setTestLoading(null);
    }
  };

  const handleViewLogs = (webhook: Webhook) => {
    setSelectedWebhook(webhook);
    setDrawerVisible(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      if (editingWebhook) {
        const dto: UpdateWebhookDto = {
          name: values.name,
          url: values.url,
          description: values.description,
          events: values.events,
          secret: values.secret,
          isActive: values.isActive,
          maxRetries: values.maxRetries,
          timeoutSeconds: values.timeoutSeconds,
          customHeaders: values.customHeaders,
        };
        await webhooksApi.updateWebhook(editingWebhook.id, dto);
        message.success(t('webhooks.updateSuccess'));
      } else {
        const dto: CreateWebhookDto = {
          name: values.name,
          url: values.url,
          description: values.description,
          events: values.events,
          secret: values.secret,
          isActive: values.isActive,
          maxRetries: values.maxRetries,
          timeoutSeconds: values.timeoutSeconds,
          customHeaders: values.customHeaders,
        };
        await webhooksApi.createWebhook(dto);
        message.success(t('webhooks.createSuccess'));
      }
      setModalVisible(false);
      loadWebhooks();
    } catch (error) {
      message.error(editingWebhook ? t('webhooks.updateFailed') : t('webhooks.createFailed'));
      console.error("Failed to save webhook:", error);
    }
  };

  const columns: ColumnsType<Webhook> = [
    {
      title: t('webhooks.name'),
      dataIndex: "name",
      key: "name",
      width: 200,
      render: (text, record) => (
        <Space direction="vertical" size={0}>
          <strong>{text}</strong>
          {record.description && (
            <span style={{ fontSize: 12, color: "#888" }}>{record.description}</span>
          )}
        </Space>
      ),
    },
    {
      title: t('webhooks.url'),
      dataIndex: "url",
      key: "url",
      ellipsis: true,
      width: 300,
    },
    {
      title: t('webhooks.events'),
      dataIndex: "events",
      key: "events",
      width: 200,
      render: (events: string[]) => (
        <Space wrap size={[0, 4]}>
          {events.slice(0, 2).map((event) => (
            <Tag key={event} color="blue" style={{ fontSize: 11 }}>
              {eventTypes.find((et) => et.value === event)?.label || event}
            </Tag>
          ))}
          {events.length > 2 && (
            <Tag color="default" style={{ fontSize: 11 }}>+{events.length - 2}</Tag>
          )}
        </Space>
      ),
    },
    {
      title: t('webhooks.status'),
      dataIndex: "isActive",
      key: "isActive",
      width: 80,
      render: (isActive: boolean) =>
        isActive ? (
          <Tag icon={<CheckCircleOutlined />} color="success">{t('webhooks.enabled')}</Tag>
        ) : (
          <Tag icon={<CloseCircleOutlined />} color="default">{t('webhooks.disabled')}</Tag>
        ),
    },
    {
      title: t('webhooks.statistics'),
      key: "stats",
      width: 150,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontSize: 12 }}>
            {t('webhooks.successCount')}: <strong style={{ color: "#52c41a" }}>{record.successCount}</strong> / {record.totalTriggers}
          </span>
          {record.failureCount > 0 && (
            <span style={{ fontSize: 12, color: "#ff4d4f" }}>
              {t('webhooks.consecutiveFailures')}: {record.failureCount}
            </span>
          )}
        </Space>
      ),
    },
    {
      title: t('webhooks.lastTriggered'),
      dataIndex: "lastTriggeredAt",
      key: "lastTriggeredAt",
      width: 120,
      render: (date: string) => date ? dayjs(date).fromNow() : "-",
    },
    {
      title: t('webhooks.actions'),
      key: "actions",
      width: 220,
      render: (_, record) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<ThunderboltOutlined />}
            onClick={() => handleTest(record)}
            loading={testLoading === record.id}
          >
            {t('webhooks.test')}
          </Button>
          <Button
            type="link"
            size="small"
            icon={<HistoryOutlined />}
            onClick={() => handleViewLogs(record)}
          >
            {t('webhooks.logs')}
          </Button>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            {t('common.edit')}
          </Button>
          <Popconfirm
            title={t('webhooks.confirmDelete')}
            onConfirm={() => handleDelete(record.id)}
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              {t('common.delete')}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Space direction="vertical" size="large" style={{ width: "100%" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <Title level={2}><ApiOutlined /> {t('webhooks.title')}</Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            {t('webhooks.create')}
          </Button>
        </div>

        <Card>
          <Row gutter={16}>
            <Col span={6}>
              <Statistic title={t('webhooks.totalWebhooks')} value={webhooks.length} />
            </Col>
            <Col span={6}>
              <Statistic
                title={t('webhooks.activeWebhooks')}
                value={webhooks.filter((w) => w.isActive).length}
                valueStyle={{ color: "#3f8600" }}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title={t('webhooks.totalTriggers')}
                value={webhooks.reduce((sum, w) => sum + w.totalTriggers, 0)}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title={t('webhooks.totalSuccesses')}
                value={webhooks.reduce((sum, w) => sum + w.successCount, 0)}
                valueStyle={{ color: "#52c41a" }}
              />
            </Col>
          </Row>
        </Card>

        <Table
          columns={columns}
          dataSource={webhooks}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10, showSizeChanger: true, showTotal: (total) => t('webhooks.total', { total }) }}
        />
      </Space>

      {/* 创建/编辑 Modal */}
      <Modal
        title={editingWebhook ? t('webhooks.editTitle') : t('webhooks.createTitle')}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={800}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t('webhooks.nameLabel')}
            name="name"
            rules={[{ required: true, message: t('webhooks.nameRequired') }]}
          >
            <Input placeholder={t('webhooks.namePlaceholder')} />
          </Form.Item>

          <Form.Item
            label={t('webhooks.urlLabel')}
            name="url"
            rules={[
              { required: true, message: t('webhooks.urlRequired') },
              { type: "url", message: t('webhooks.urlInvalid') },
            ]}
          >
            <Input placeholder={t('webhooks.urlPlaceholder')} />
          </Form.Item>

          <Form.Item label={t('webhooks.descriptionLabel')} name="description">
            <TextArea rows={2} placeholder={t('webhooks.descriptionPlaceholder')} />
          </Form.Item>

          <Form.Item
            label={t('webhooks.eventsLabel')}
            name="events"
            rules={[{ required: true, message: t('webhooks.eventsRequired') }]}
          >
            <Select
              mode="multiple"
              placeholder={t('webhooks.eventsPlaceholder')}
              options={eventTypes.map((et) => ({ label: et.label, value: et.value }))}
              maxTagCount="responsive"
            />
          </Form.Item>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item label={t('webhooks.secretLabel')} name="secret">
                <Input.Password placeholder={t('webhooks.secretPlaceholder')} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label={t('webhooks.enabledLabel')} name="isActive" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item label={t('webhooks.maxRetriesLabel')} name="maxRetries">
                <InputNumber min={0} max={10} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label={t('webhooks.timeoutLabel')} name="timeoutSeconds">
                <InputNumber min={5} max={300} style={{ width: "100%" }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label={t('webhooks.customHeadersLabel')} name="customHeaders">
            <TextArea
              rows={3}
              placeholder={t('webhooks.customHeadersPlaceholder')}
            />
          </Form.Item>
        </Form>
      </Modal>

      {/* 日志 Drawer */}
      <Drawer
        title={t('webhooks.logsTitle', { name: selectedWebhook?.name || '' })}
        placement="right"
        width={800}
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
      >
        {selectedWebhook && <WebhookLogsViewer webhookId={selectedWebhook.id} />}
      </Drawer>
    </div>
  );
}

// Webhook日志查看组件
interface WebhookLogsViewerProps {
  webhookId: string;
}

function WebhookLogsViewer({ webhookId }: WebhookLogsViewerProps) {
  const { t } = useTranslation();
  const [logs, setLogs] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadLogs();
  }, [webhookId]);

  const loadLogs = async () => {
    setLoading(true);
    try {
      const data = await webhooksApi.getWebhookLogs(webhookId);
      setLogs(data);
    } catch (error) {
      message.error(t('webhooks.loadLogsFailed'));
      console.error("Failed to load logs:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async (logId: string) => {
    try {
      await webhooksApi.retryWebhookLog(logId);
      message.success(t('webhooks.retrySuccess'));
      setTimeout(loadLogs, 1000);
    } catch (error) {
      message.error(t('webhooks.retryFailed'));
      console.error("Failed to retry:", error);
    }
  };

  const logColumns: ColumnsType<any> = [
    {
      title: t('webhooks.logsTime'),
      dataIndex: "createdAt",
      key: "createdAt",
      width: 180,
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
    },
    {
      title: t('webhooks.logsEvent'),
      dataIndex: "eventType",
      key: "eventType",
      width: 150,
    },
    {
      title: t('webhooks.logsStatus'),
      dataIndex: "success",
      key: "success",
      width: 80,
      render: (success: boolean, record: any) => (
        <Tag color={success ? "success" : "error"}>
          {success ? t('webhooks.logsSuccess') : `${t('webhooks.logsFailed')} (${record.statusCode})`}
        </Tag>
      ),
    },
    {
      title: t('webhooks.logsResponseTime'),
      dataIndex: "durationMs",
      key: "durationMs",
      width: 100,
      render: (ms: number) => `${ms}ms`,
    },
    {
      title: t('webhooks.logsRetry'),
      dataIndex: "retryCount",
      key: "retryCount",
      width: 60,
      render: (count: number) => count || 0,
    },
    {
      title: t('common.actions'),
      key: "actions",
      width: 100,
      render: (_, record) => (
        !record.success && (
          <Button size="small" onClick={() => handleRetry(record.id)}>
            {t('webhooks.logsRetryButton')}
          </Button>
        )
      ),
    },
  ];

  if (loading) {
    return <Spin tip={t('common.loading')} style={{ width: "100%", marginTop: 50 }} />;
  }

  return (
    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
      <Table
        columns={logColumns}
        dataSource={logs}
        rowKey="id"
        pagination={{ pageSize: 20 }}
        expandable={{
          expandedRowRender: (record) => (
            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label={t('webhooks.logsPayload')}>
                <pre style={{ maxHeight: 200, overflow: "auto", fontSize: 11 }}>
                  {JSON.stringify(JSON.parse(record.payload), null, 2)}
                </pre>
              </Descriptions.Item>
              {record.response && (
                <Descriptions.Item label={t('webhooks.logsResponse')}>
                  <pre style={{ maxHeight: 200, overflow: "auto", fontSize: 11 }}>
                    {record.response}
                  </pre>
                </Descriptions.Item>
              )}
              {record.errorMessage && (
                <Descriptions.Item label={t('webhooks.logsError')}>
                  <span style={{ color: "#ff4d4f" }}>{record.errorMessage}</span>
                </Descriptions.Item>
              )}
            </Descriptions>
          ),
        }}
      />
    </Space>
  );
}

