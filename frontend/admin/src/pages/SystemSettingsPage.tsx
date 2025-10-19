import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Card,
  Tabs,
  Table,
  Button,
  Form,
  Input,
  InputNumber,
  Switch,
  Select,
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
  SettingOutlined,
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
  PlusOutlined,
  SaveOutlined,
  CheckCircleOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { systemSettingsApi, type SystemSetting } from "../lib/systemSettingsApi";
import dayjs from "dayjs";

const { Title, Text, Paragraph } = Typography;
const { TextArea } = Input;
const { TabPane } = Tabs;

export default function SystemSettingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [selectedGroup, setSelectedGroup] = useState<string | undefined>(undefined);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [selectedSetting, setSelectedSetting] = useState<SystemSetting | null>(null);
  const [form] = Form.useForm();
  const [createForm] = Form.useForm();

  // 获取所有设置
  const { data: settings, isLoading } = useQuery({
    queryKey: ["systemSettings", selectedGroup],
    queryFn: () => systemSettingsApi.getAll(selectedGroup),
  });

  // 更新设置
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      systemSettingsApi.update(id, data),
    onSuccess: () => {
      message.success("Setting updated successfully");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
      setEditModalOpen(false);
      setSelectedSetting(null);
    },
    onError: (error: Error) => {
      message.error(`Failed to update setting: ${error.message}`);
    },
  });

  // 创建设置
  const createMutation = useMutation({
    mutationFn: (data: any) => systemSettingsApi.create(data),
    onSuccess: () => {
      message.success("Setting created successfully");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
      setCreateModalOpen(false);
      createForm.resetFields();
    },
    onError: (error: Error) => {
      message.error(`Failed to create setting: ${error.message}`);
    },
  });

  // 删除设置
  const deleteMutation = useMutation({
    mutationFn: (id: number) => systemSettingsApi.delete(id),
    onSuccess: () => {
      message.success("Setting deleted successfully");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`Failed to delete setting: ${error.message}`);
    },
  });

  // 重置设置
  const resetMutation = useMutation({
    mutationFn: (key: string) => systemSettingsApi.reset(key),
    onSuccess: () => {
      message.success("Setting reset to default value");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`Failed to reset setting: ${error.message}`);
    },
  });

  // 重置分组
  const resetGroupMutation = useMutation({
    mutationFn: (group: string) => systemSettingsApi.resetGroup(group),
    onSuccess: () => {
      message.success("Settings group reset to default values");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`Failed to reset group: ${error.message}`);
    },
  });

  // 确保默认设置
  const ensureDefaultsMutation = useMutation({
    mutationFn: () => systemSettingsApi.ensureDefaults(),
    onSuccess: () => {
      message.success("Default settings ensured");
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`Failed to ensure defaults: ${error.message}`);
    },
  });

  // 打开编辑对话框
  const handleEdit = (setting: SystemSetting) => {
    setSelectedSetting(setting);
    form.setFieldsValue({
      value: setting.value,
      displayName: setting.displayName,
      description: setting.description,
    });
    setEditModalOpen(true);
  };

  // 提交编辑
  const handleEditSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (selectedSetting) {
        updateMutation.mutate({ id: selectedSetting.id, data: values });
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

  // 按分组获取设置
  const settingsByGroup = settings?.reduce((acc, setting) => {
    if (!acc[setting.group]) {
      acc[setting.group] = [];
    }
    acc[setting.group].push(setting);
    return acc;
  }, {} as Record<string, SystemSetting[]>) || {};

  const groups = Object.keys(settingsByGroup).sort();

  // 表格列定义
  const columns: ColumnsType<SystemSetting> = [
    {
      title: "Setting",
      key: "setting",
      width: 250,
      render: (_, record) => (
        <div>
          <div style={{ fontWeight: 600, marginBottom: 4 }}>
            {record.displayName || record.key}
            {record.isReadOnly && (
              <Tag color="orange" style={{ marginLeft: 8 }}>
                Read-Only
              </Tag>
            )}
            {record.isSensitive && (
              <Tag color="red" style={{ marginLeft: 8 }}>
                Sensitive
              </Tag>
            )}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {record.key}
          </Text>
          {record.description && (
            <div style={{ fontSize: 12, color: "#666", marginTop: 4 }}>
              {record.description}
            </div>
          )}
        </div>
      ),
    },
    {
      title: "Value",
      key: "value",
      width: 200,
      render: (_, record) => {
        if (record.isSensitive) {
          return <Text type="secondary">••••••••</Text>;
        }
        
        if (record.valueType === "Boolean") {
          return (
            <Tag color={record.value === "True" ? "green" : "default"}>
              {record.value === "True" ? "Enabled" : "Disabled"}
            </Tag>
          );
        }

        return (
          <Text code style={{ fontSize: 13 }}>
            {record.value}
          </Text>
        );
      },
    },
    {
      title: "Type",
      dataIndex: "valueType",
      key: "valueType",
      width: 100,
      render: (type: string) => <Tag>{type}</Tag>,
    },
    {
      title: "Default",
      dataIndex: "defaultValue",
      key: "defaultValue",
      width: 120,
      render: (value: string) => (
        <Text type="secondary" style={{ fontSize: 12 }}>
          {value || "-"}
        </Text>
      ),
    },
    {
      title: "Last Modified",
      key: "modified",
      width: 150,
      render: (_, record) => (
        <div>
          <div style={{ fontSize: 12 }}>
            {record.updatedAt
              ? dayjs(record.updatedAt).format("YYYY-MM-DD HH:mm")
              : dayjs(record.createdAt).format("YYYY-MM-DD HH:mm")}
          </div>
          {record.lastModifiedBy && (
            <Text type="secondary" style={{ fontSize: 11 }}>
              by {record.lastModifiedBy}
            </Text>
          )}
        </div>
      ),
    },
    {
      title: "Actions",
      key: "actions",
      width: 180,
      fixed: "right",
      render: (_, record) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
            disabled={record.isReadOnly}
          >
            Edit
          </Button>
          {record.defaultValue && (
            <Popconfirm
              title="Reset to default value?"
              description="This will restore the setting to its default value."
              onConfirm={() => resetMutation.mutate(record.key)}
              okText="Reset"
              cancelText="Cancel"
            >
              <Button
                type="link"
                size="small"
                icon={<ReloadOutlined />}
                disabled={record.isReadOnly || record.value === record.defaultValue}
              >
                Reset
              </Button>
            </Popconfirm>
          )}
          {!record.isReadOnly && (
            <Popconfirm
              title="Delete this setting?"
              description="This action cannot be undone."
              onConfirm={() => deleteMutation.mutate(record.id)}
              okText="Delete"
              okType="danger"
              cancelText="Cancel"
            >
              <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                Delete
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <div style={{ marginBottom: 24 }}>
        <Title level={2}>
          <SettingOutlined /> System Settings
        </Title>
        <Paragraph type="secondary">
          Configure system-wide settings for authentication, security, sessions, and more.
        </Paragraph>

        <Alert
          message="Important"
          description="Changes to system settings will affect all users and may require application restart for some settings to take effect."
          type="warning"
          showIcon
          icon={<WarningOutlined />}
          style={{ marginBottom: 16 }}
        />

        <Space style={{ marginBottom: 16 }}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setCreateModalOpen(true)}
          >
            Create Setting
          </Button>
          <Button
            icon={<CheckCircleOutlined />}
            onClick={() => ensureDefaultsMutation.mutate()}
            loading={ensureDefaultsMutation.isPending}
          >
            Ensure Defaults
          </Button>
          {selectedGroup && (
            <Popconfirm
              title={`Reset all settings in ${selectedGroup} group?`}
              description="This will restore all settings in this group to their default values."
              onConfirm={() => resetGroupMutation.mutate(selectedGroup)}
              okText="Reset Group"
              okType="danger"
              cancelText="Cancel"
            >
              <Button
                icon={<ReloadOutlined />}
                loading={resetGroupMutation.isPending}
                danger
              >
                Reset Group
              </Button>
            </Popconfirm>
          )}
        </Space>
      </div>

      <Card>
        <Tabs
          activeKey={selectedGroup}
          onChange={setSelectedGroup}
          items={[
            {
              key: undefined as any,
              label: `All (${settings?.length || 0})`,
            },
            ...groups.map((group) => ({
              key: group,
              label: `${group} (${settingsByGroup[group]?.length || 0})`,
            })),
          ]}
        />

        <Table
          columns={columns}
          dataSource={settings}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 20, showSizeChanger: true, showTotal: (total) => `Total ${total} settings` }}
          scroll={{ x: 1200 }}
        />
      </Card>

      {/* 编辑设置对话框 */}
      <Modal
        title={
          <div>
            <EditOutlined /> Edit Setting
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
              <Descriptions.Item label="Key">{selectedSetting.key}</Descriptions.Item>
              <Descriptions.Item label="Group">{selectedSetting.group}</Descriptions.Item>
              <Descriptions.Item label="Type">{selectedSetting.valueType}</Descriptions.Item>
            </Descriptions>

            <Form form={form} layout="vertical">
              <Form.Item
                label="Value"
                name="value"
                rules={[{ required: true, message: "Please enter a value" }]}
              >
                {selectedSetting.valueType === "Boolean" ? (
                  <Select>
                    <Select.Option value="True">True</Select.Option>
                    <Select.Option value="False">False</Select.Option>
                  </Select>
                ) : selectedSetting.valueType === "Integer" ? (
                  <InputNumber style={{ width: "100%" }} />
                ) : (
                  <Input />
                )}
              </Form.Item>

              <Form.Item label="Display Name" name="displayName">
                <Input placeholder="Optional display name" />
              </Form.Item>

              <Form.Item label="Description" name="description">
                <TextArea rows={3} placeholder="Optional description" />
              </Form.Item>
            </Form>
          </>
        )}
      </Modal>

      {/* 创建设置对话框 */}
      <Modal
        title={
          <div>
            <PlusOutlined /> Create Setting
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
            label="Key"
            name="key"
            rules={[{ required: true, message: "Please enter a key" }]}
          >
            <Input placeholder="e.g., Feature.EnableNewFeature" />
          </Form.Item>

          <Form.Item
            label="Group"
            name="group"
            rules={[{ required: true, message: "Please select a group" }]}
          >
            <Select placeholder="Select a group">
              <Select.Option value="Authentication">Authentication</Select.Option>
              <Select.Option value="Security">Security</Select.Option>
              <Select.Option value="Password">Password</Select.Option>
              <Select.Option value="TokenLifetime">TokenLifetime</Select.Option>
              <Select.Option value="Session">Session</Select.Option>
              <Select.Option value="Registration">Registration</Select.Option>
              <Select.Option value="General">General</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            label="Value Type"
            name="valueType"
            initialValue="String"
            rules={[{ required: true, message: "Please select a value type" }]}
          >
            <Select>
              <Select.Option value="String">String</Select.Option>
              <Select.Option value="Integer">Integer</Select.Option>
              <Select.Option value="Boolean">Boolean</Select.Option>
              <Select.Option value="Decimal">Decimal</Select.Option>
              <Select.Option value="JSON">JSON</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            label="Value"
            name="value"
            rules={[{ required: true, message: "Please enter a value" }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label="Display Name" name="displayName">
            <Input placeholder="Optional display name" />
          </Form.Item>

          <Form.Item label="Description" name="description">
            <TextArea rows={3} placeholder="Optional description" />
          </Form.Item>

          <Form.Item label="Default Value" name="defaultValue">
            <Input placeholder="Optional default value" />
          </Form.Item>

          <Form.Item label="Sort Order" name="sortOrder" initialValue={0}>
            <InputNumber style={{ width: "100%" }} min={0} />
          </Form.Item>

          <Form.Item name="isSensitive" valuePropName="checked" initialValue={false}>
            <Switch /> Sensitive (hide value in UI)
          </Form.Item>

          <Form.Item name="isReadOnly" valuePropName="checked" initialValue={false}>
            <Switch /> Read-Only (prevent modification)
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

