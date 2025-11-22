import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Card,
  Table,
  Button,
  Form,
  Input,
  InputNumber,
  Switch,
  Modal,
  message,
  Typography,
  Space,
  Tag,
  Popconfirm,
  Descriptions,
  Alert,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
  ThunderboltOutlined,
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import {
  rateLimitSettingsApi,
  type RateLimitSetting,
} from "../lib/rateLimitSettingsApi";

const { Title, Text, Paragraph } = Typography;
const { TextArea } = Input;

export default function RateLimitSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [selectedSetting, setSelectedSetting] =
    useState<RateLimitSetting | null>(null);
  const [form] = Form.useForm();
  const [createForm] = Form.useForm();

  // 获取所有设置
  const { data: settings, isLoading } = useQuery({
    queryKey: ["rateLimitSettings"],
    queryFn: () => rateLimitSettingsApi.getAll(),
  });

  // 更新设置
  const updateMutation = useMutation({
    mutationFn: (data: any) => rateLimitSettingsApi.update(data),
    onSuccess: () => {
      message.success(t("rateLimitSettings.updateSuccess"));
      queryClient.invalidateQueries({ queryKey: ["rateLimitSettings"] });
      setEditModalOpen(false);
      setSelectedSetting(null);
    },
    onError: (error: Error) => {
      message.error(`${t("rateLimitSettings.updateFailed")}: ${error.message}`);
    },
  });

  // 创建设置
  const createMutation = useMutation({
    mutationFn: (data: any) => rateLimitSettingsApi.create(data),
    onSuccess: () => {
      message.success(t("rateLimitSettings.createSuccess"));
      queryClient.invalidateQueries({ queryKey: ["rateLimitSettings"] });
      setCreateModalOpen(false);
      createForm.resetFields();
    },
    onError: (error: Error) => {
      message.error(`${t("rateLimitSettings.createFailed")}: ${error.message}`);
    },
  });

  // 删除设置
  const deleteMutation = useMutation({
    mutationFn: (id: number) => rateLimitSettingsApi.delete(id),
    onSuccess: () => {
      message.success(t("rateLimitSettings.deleteSuccess"));
      queryClient.invalidateQueries({ queryKey: ["rateLimitSettings"] });
    },
    onError: (error: Error) => {
      message.error(`${t("rateLimitSettings.deleteFailed")}: ${error.message}`);
    },
  });

  // 确保默认设置
  const ensureDefaultsMutation = useMutation({
    mutationFn: () => rateLimitSettingsApi.ensureDefaults(),
    onSuccess: () => {
      message.success(t("rateLimitSettings.ensureDefaultsSuccess"));
      queryClient.invalidateQueries({ queryKey: ["rateLimitSettings"] });
    },
    onError: (error: Error) => {
      message.error(
        `${t("rateLimitSettings.ensureDefaultsFailed")}: ${error.message}`
      );
    },
  });

  // 打开编辑对话框
  const handleEdit = (setting: RateLimitSetting) => {
    setSelectedSetting(setting);
    form.setFieldsValue(setting);
    setEditModalOpen(true);
  };

  // 提交编辑
  const handleEditSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (selectedSetting) {
        updateMutation.mutate({ id: selectedSetting.id, ...values });
      }
    } catch (error) {
      console.error("Validation failed:", error);
    }
  };

  // 提交创建
  const handleCreateSubmit = async () => {
    try {
      const values = await createForm.validateFields();
      createMutation.mutate(values);
    } catch (error) {
      console.error("Validation failed:", error);
    }
  };

  // 格式化时间窗口显示
  const formatWindowSeconds = (seconds: number) => {
    if (seconds < 60) return `${seconds}${t("rateLimitSettings.seconds")}`;
    if (seconds < 3600)
      return `${Math.floor(seconds / 60)}${t("rateLimitSettings.minutes")}`;
    return `${Math.floor(seconds / 3600)}${t("rateLimitSettings.hours")}`;
  };

  // 表格列定义
  const columns: ColumnsType<RateLimitSetting> = [
    {
      title: t("rateLimitSettings.limiterName"),
      dataIndex: "limiterName",
      key: "limiterName",
      width: 150,
      render: (name: string, record) => (
        <div>
          <div style={{ fontWeight: 600 }}>
            {name}
            {!record.enabled && (
              <Tag color="default" style={{ marginLeft: 8 }}>
                {t("rateLimitSettings.disabled")}
              </Tag>
            )}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {record.displayName}
          </Text>
        </div>
      ),
    },
    {
      title: t("rateLimitSettings.description"),
      dataIndex: "description",
      key: "description",
      ellipsis: true,
      render: (desc: string) => (
        <Text type="secondary" style={{ fontSize: 13 }}>
          {desc || "-"}
        </Text>
      ),
    },
    {
      title: t("rateLimitSettings.limitConfig"),
      key: "config",
      width: 200,
      render: (_, record) => (
        <div>
          <div>
            <Text strong>{record.permitLimit}</Text>{" "}
            {t("rateLimitSettings.requests")}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {t("rateLimitSettings.per")} {formatWindowSeconds(record.windowSeconds)}
          </Text>
        </div>
      ),
    },
    {
      title: t("rateLimitSettings.queueLimit"),
      dataIndex: "queueLimit",
      key: "queueLimit",
      width: 100,
      align: "center",
    },
    {
      title: t("rateLimitSettings.status"),
      key: "enabled",
      width: 100,
      align: "center",
      render: (_, record) => (
        <Tag color={record.enabled ? "green" : "default"}>
          {record.enabled
            ? t("rateLimitSettings.enabled")
            : t("rateLimitSettings.disabled")}
        </Tag>
      ),
    },
    {
      title: t("rateLimitSettings.actions"),
      key: "actions",
      width: 150,
      fixed: "right",
      render: (_, record) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            {t("rateLimitSettings.edit")}
          </Button>
          <Popconfirm
            title={t("rateLimitSettings.deleteConfirm")}
            description={t("rateLimitSettings.deleteDescription")}
            onConfirm={() => deleteMutation.mutate(record.id)}
            okText={t("rateLimitSettings.delete")}
            okType="danger"
            cancelText={t("common.cancel")}
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              {t("rateLimitSettings.delete")}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <div style={{ marginBottom: 24 }}>
        <Title level={2}>
          <ThunderboltOutlined /> {t("rateLimitSettings.title")}
        </Title>
        <Paragraph type="secondary">
          {t("rateLimitSettings.subtitle")}
        </Paragraph>

        <Alert
          message={t("rateLimitSettings.restartRequired")}
          description={t("rateLimitSettings.restartRequiredDescription")}
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
        />

        <Alert
          message={t("rateLimitSettings.warningTitle")}
          description={t("rateLimitSettings.warningMessage")}
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
        />

        <Space style={{ marginBottom: 16 }}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setCreateModalOpen(true)}
          >
            {t("rateLimitSettings.createSetting")}
          </Button>
          <Button
            icon={<CheckCircleOutlined />}
            onClick={() => ensureDefaultsMutation.mutate()}
            loading={ensureDefaultsMutation.isPending}
          >
            {t("rateLimitSettings.ensureDefaults")}
          </Button>
        </Space>
      </div>

      <Card>
        <Table
          columns={columns}
          dataSource={settings}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 20, showSizeChanger: true }}
          scroll={{ x: 1000 }}
        />
      </Card>

      {/* 编辑设置对话框 */}
      <Modal
        title={
          <div>
            <EditOutlined /> {t("rateLimitSettings.editSetting")}
          </div>
        }
        open={editModalOpen}
        onOk={handleEditSubmit}
        onCancel={() => {
          setEditModalOpen(false);
          setSelectedSetting(null);
          form.resetFields();
        }}
        confirmLoading={updateMutation.isPending}
        width={600}
      >
        {selectedSetting && (
          <>
            <Descriptions bordered column={1} size="small" style={{ marginBottom: 16 }}>
              <Descriptions.Item label={t("rateLimitSettings.limiterName")}>
                {selectedSetting.limiterName}
              </Descriptions.Item>
            </Descriptions>

            <Form form={form} layout="vertical">
              <Form.Item
                label={t("rateLimitSettings.displayName")}
                name="displayName"
                rules={[
                  {
                    required: true,
                    message: t("rateLimitSettings.displayNameRequired"),
                  },
                ]}
              >
                <Input />
              </Form.Item>

              <Form.Item
                label={t("rateLimitSettings.description")}
                name="description"
              >
                <TextArea rows={3} />
              </Form.Item>

              <Form.Item
                label={t("rateLimitSettings.permitLimit")}
                name="permitLimit"
                rules={[
                  {
                    required: true,
                    message: t("rateLimitSettings.permitLimitRequired"),
                  },
                  {
                    type: "number",
                    min: 1,
                    message: t("rateLimitSettings.permitLimitMin"),
                  },
                ]}
              >
                <InputNumber min={1} style={{ width: "100%" }} placeholder="e.g., 100" />
              </Form.Item>

              <Form.Item
                label={t("rateLimitSettings.windowSeconds")}
                name="windowSeconds"
                rules={[
                  {
                    required: true,
                    message: t("rateLimitSettings.windowSecondsRequired"),
                  },
                  {
                    type: "number",
                    min: 1,
                    message: t("rateLimitSettings.windowSecondsMin"),
                  },
                ]}
              >
                <InputNumber min={1} style={{ width: "100%" }} placeholder="e.g., 60" />
              </Form.Item>

              <Form.Item
                label={t("rateLimitSettings.queueLimit")}
                name="queueLimit"
                rules={[
                  {
                    type: "number",
                    min: 0,
                    message: t("rateLimitSettings.queueLimitMin"),
                  },
                ]}
              >
                <InputNumber min={0} style={{ width: "100%" }} placeholder="0" />
              </Form.Item>

              <Form.Item
                label={t("rateLimitSettings.sortOrder")}
                name="sortOrder"
              >
                <InputNumber min={0} style={{ width: "100%" }} />
              </Form.Item>

              <Form.Item name="enabled" valuePropName="checked">
                <Switch /> {t("rateLimitSettings.enabled")}
              </Form.Item>
            </Form>
          </>
        )}
      </Modal>

      {/* 创建设置对话框 */}
      <Modal
        title={
          <div>
            <PlusOutlined /> {t("rateLimitSettings.createTitle")}
          </div>
        }
        open={createModalOpen}
        onOk={handleCreateSubmit}
        onCancel={() => {
          setCreateModalOpen(false);
          createForm.resetFields();
        }}
        confirmLoading={createMutation.isPending}
        width={600}
      >
        <Form form={createForm} layout="vertical">
          <Form.Item
            label={t("rateLimitSettings.limiterName")}
            name="limiterName"
            rules={[
              {
                required: true,
                message: t("rateLimitSettings.limiterNameRequired"),
              },
            ]}
          >
            <Input placeholder="e.g., api, webhook, export" />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.displayName")}
            name="displayName"
            rules={[
              {
                required: true,
                message: t("rateLimitSettings.displayNameRequired"),
              },
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.description")}
            name="description"
          >
            <TextArea rows={3} />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.permitLimit")}
            name="permitLimit"
            initialValue={100}
            rules={[
              {
                required: true,
                message: t("rateLimitSettings.permitLimitRequired"),
              },
              {
                type: "number",
                min: 1,
                message: t("rateLimitSettings.permitLimitMin"),
              },
            ]}
          >
            <InputNumber min={1} style={{ width: "100%" }} placeholder="e.g., 100" />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.windowSeconds")}
            name="windowSeconds"
            initialValue={60}
            rules={[
              {
                required: true,
                message: t("rateLimitSettings.windowSecondsRequired"),
              },
              {
                type: "number",
                min: 1,
                message: t("rateLimitSettings.windowSecondsMin"),
              },
            ]}
          >
            <InputNumber min={1} style={{ width: "100%" }} placeholder="e.g., 60" />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.queueLimit")}
            name="queueLimit"
            initialValue={0}
            rules={[
              {
                type: "number",
                min: 0,
                message: t("rateLimitSettings.queueLimitMin"),
              },
            ]}
          >
            <InputNumber min={0} style={{ width: "100%" }} placeholder="0" />
          </Form.Item>

          <Form.Item
            label={t("rateLimitSettings.sortOrder")}
            name="sortOrder"
            initialValue={0}
          >
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>

          <Form.Item name="enabled" valuePropName="checked" initialValue={true}>
            <Switch /> {t("rateLimitSettings.enabled")}
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
