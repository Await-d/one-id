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

  // 获取所有设置（不传递 selectedGroup，始终获取全部）
  const { data: allSettings, isLoading } = useQuery({
    queryKey: ["systemSettings"],
    queryFn: () => systemSettingsApi.getAll(undefined),
  });

  // 根据选中的分组过滤设置
  const settings = selectedGroup
    ? allSettings?.filter((s) => s.group === selectedGroup)
    : allSettings;

  // 更新设置
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      systemSettingsApi.update(id, data),
    onSuccess: () => {
      message.success(t('systemSettings.updateSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
      setEditModalOpen(false);
      setSelectedSetting(null);
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.updateFailed')}: ${error.message}`);
    },
  });

  // 创建设置
  const createMutation = useMutation({
    mutationFn: (data: any) => systemSettingsApi.create(data),
    onSuccess: () => {
      message.success(t('systemSettings.createSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
      setCreateModalOpen(false);
      createForm.resetFields();
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.createFailed')}: ${error.message}`);
    },
  });

  // 删除设置
  const deleteMutation = useMutation({
    mutationFn: (id: number) => systemSettingsApi.delete(id),
    onSuccess: () => {
      message.success(t('systemSettings.deleteSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.deleteFailed')}: ${error.message}`);
    },
  });

  // 重置设置
  const resetMutation = useMutation({
    mutationFn: (key: string) => systemSettingsApi.reset(key),
    onSuccess: () => {
      message.success(t('systemSettings.resetSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.resetFailed')}: ${error.message}`);
    },
  });

  // 重置分组
  const resetGroupMutation = useMutation({
    mutationFn: (group: string) => systemSettingsApi.resetGroup(group),
    onSuccess: () => {
      message.success(t('systemSettings.resetGroupSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.resetGroupFailed')}: ${error.message}`);
    },
  });

  // 确保默认设置
  const ensureDefaultsMutation = useMutation({
    mutationFn: () => systemSettingsApi.ensureDefaults(),
    onSuccess: () => {
      message.success(t('systemSettings.ensureDefaultsSuccess'));
      queryClient.invalidateQueries({ queryKey: ["systemSettings"] });
    },
    onError: (error: Error) => {
      message.error(`${t('systemSettings.ensureDefaultsFailed')}: ${error.message}`);
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

  // 按分组获取设置（基于所有设置，不受过滤影响）
  const settingsByGroup = allSettings?.reduce((acc, setting) => {
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
      title: t('systemSettings.setting'),
      key: "setting",
      width: 250,
      render: (_, record) => (
        <div>
          <div style={{ fontWeight: 600, marginBottom: 4 }}>
            {t(`systemSettings.settingDisplayNames.${record.key}`, record.displayName || record.key)}
            {record.isReadOnly && (
              <Tag color="orange" style={{ marginLeft: 8 }}>
                {t('systemSettings.readOnly')}
              </Tag>
            )}
            {record.isSensitive && (
              <Tag color="red" style={{ marginLeft: 8 }}>
                {t('systemSettings.sensitive')}
              </Tag>
            )}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {record.key}
          </Text>
          <div style={{ fontSize: 12, color: "#666", marginTop: 4 }}>
            {t(`systemSettings.settingDescriptions.${record.key}`, record.description || '')}
          </div>
        </div>
      ),
    },
    {
      title: t('systemSettings.value'),
      key: "value",
      width: 200,
      render: (_, record) => {
        if (record.isSensitive) {
          return <Text type="secondary">••••••••</Text>;
        }

        if (record.valueType === "Boolean") {
          return (
            <Tag color={record.value === "True" ? "green" : "default"}>
              {record.value === "True" ? t('systemSettings.enabled') : t('systemSettings.disabled')}
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
      title: t('systemSettings.type'),
      dataIndex: "valueType",
      key: "valueType",
      width: 100,
      render: (type: string) => <Tag>{t(`systemSettings.valueTypes.${type}`, type)}</Tag>,
    },
    {
      title: t('systemSettings.default'),
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
      title: t('systemSettings.lastModified'),
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
      title: t('systemSettings.actions'),
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
            {t('systemSettings.edit')}
          </Button>
          {record.defaultValue && (
            <Popconfirm
              title={t('systemSettings.resetConfirm')}
              description={t('systemSettings.resetDescription')}
              onConfirm={() => resetMutation.mutate(record.key)}
              okText={t('systemSettings.reset')}
              cancelText={t('common.cancel')}
            >
              <Button
                type="link"
                size="small"
                icon={<ReloadOutlined />}
                disabled={record.isReadOnly || record.value === record.defaultValue}
              >
                {t('systemSettings.reset')}
              </Button>
            </Popconfirm>
          )}
          {!record.isReadOnly && (
            <Popconfirm
              title={t('systemSettings.deleteConfirm')}
              description={t('systemSettings.deleteDescription')}
              onConfirm={() => deleteMutation.mutate(record.id)}
              okText={t('systemSettings.delete')}
              okType="danger"
              cancelText={t('common.cancel')}
            >
              <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                {t('systemSettings.delete')}
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
          <SettingOutlined /> {t('systemSettings.title')}
        </Title>
        <Paragraph type="secondary">
          {t('systemSettings.subtitle')}
        </Paragraph>

        <Alert
          message={t('systemSettings.importantTitle')}
          description={t('systemSettings.importantMessage')}
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
            {t('systemSettings.createSetting')}
          </Button>
          <Button
            icon={<CheckCircleOutlined />}
            onClick={() => ensureDefaultsMutation.mutate()}
            loading={ensureDefaultsMutation.isPending}
          >
            {t('systemSettings.ensureDefaults')}
          </Button>
          {selectedGroup && (
            <Popconfirm
              title={t('systemSettings.resetGroupConfirm', { group: selectedGroup })}
              description={t('systemSettings.resetGroupDescription')}
              onConfirm={() => resetGroupMutation.mutate(selectedGroup)}
              okText={t('systemSettings.resetGroup')}
              okType="danger"
              cancelText={t('common.cancel')}
            >
              <Button
                icon={<ReloadOutlined />}
                loading={resetGroupMutation.isPending}
                danger
              >
                {t('systemSettings.resetGroup')}
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
              label: `${t('systemSettings.all')} (${allSettings?.length || 0})`,
            },
            ...groups.map((group) => ({
              key: group,
              label: `${t(`systemSettings.groups.${group}`, group)} (${settingsByGroup[group]?.length || 0})`,
            })),
          ]}
        />

        <Table
          columns={columns}
          dataSource={settings}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 20, showSizeChanger: true, showTotal: (total) => t('systemSettings.totalSettings', { total }) }}
          scroll={{ x: 1200 }}
        />
      </Card>

      {/* 编辑设置对话框 */}
      <Modal
        title={
          <div>
            <EditOutlined /> {t('systemSettings.editSetting')}
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
              <Descriptions.Item label={t('systemSettings.key')}>{selectedSetting.key}</Descriptions.Item>
              <Descriptions.Item label={t('systemSettings.group')}>{t(`systemSettings.groups.${selectedSetting.group}`, selectedSetting.group)}</Descriptions.Item>
              <Descriptions.Item label={t('systemSettings.type')}>{t(`systemSettings.valueTypes.${selectedSetting.valueType}`, selectedSetting.valueType)}</Descriptions.Item>
            </Descriptions>

            <Form form={form} layout="vertical">
              <Form.Item
                label={t('systemSettings.value')}
                name="value"
                rules={[{ required: true, message: t('systemSettings.valueRequired') }]}
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

              <Form.Item label={t('systemSettings.displayName')} name="displayName">
                <Input placeholder={t('systemSettings.displayNamePlaceholder')} />
              </Form.Item>

              <Form.Item label={t('systemSettings.description')} name="description">
                <TextArea rows={3} placeholder={t('systemSettings.descriptionPlaceholder')} />
              </Form.Item>
            </Form>
          </>
        )}
      </Modal>

      {/* 创建设置对话框 */}
      <Modal
        title={
          <div>
            <PlusOutlined /> {t('systemSettings.createTitle')}
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
            label={t('systemSettings.key')}
            name="key"
            rules={[{ required: true, message: t('systemSettings.keyRequired') }]}
          >
            <Input placeholder={t('systemSettings.keyPlaceholder')} />
          </Form.Item>

          <Form.Item
            label={t('systemSettings.group')}
            name="group"
            rules={[{ required: true, message: t('systemSettings.groupRequired') }]}
          >
            <Select placeholder={t('systemSettings.groupPlaceholder')}>
              <Select.Option value="Authentication">{t('systemSettings.groups.Authentication')}</Select.Option>
              <Select.Option value="Security">{t('systemSettings.groups.Security')}</Select.Option>
              <Select.Option value="Password">{t('systemSettings.groups.Password')}</Select.Option>
              <Select.Option value="TokenLifetime">{t('systemSettings.groups.TokenLifetime')}</Select.Option>
              <Select.Option value="Session">{t('systemSettings.groups.Session')}</Select.Option>
              <Select.Option value="Registration">{t('systemSettings.groups.Registration')}</Select.Option>
              <Select.Option value="General">{t('systemSettings.groups.General')}</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            label={t('systemSettings.type')}
            name="valueType"
            initialValue="String"
            rules={[{ required: true, message: t('systemSettings.typeRequired') }]}
          >
            <Select>
              <Select.Option value="String">{t('systemSettings.valueTypes.String')}</Select.Option>
              <Select.Option value="Integer">{t('systemSettings.valueTypes.Integer')}</Select.Option>
              <Select.Option value="Boolean">{t('systemSettings.valueTypes.Boolean')}</Select.Option>
              <Select.Option value="Decimal">{t('systemSettings.valueTypes.Decimal')}</Select.Option>
              <Select.Option value="JSON">{t('systemSettings.valueTypes.JSON')}</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            label={t('systemSettings.value')}
            name="value"
            rules={[{ required: true, message: t('systemSettings.valueRequired') }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label={t('systemSettings.displayName')} name="displayName">
            <Input placeholder={t('systemSettings.displayNamePlaceholder')} />
          </Form.Item>

          <Form.Item label={t('systemSettings.description')} name="description">
            <TextArea rows={3} placeholder={t('systemSettings.descriptionPlaceholder')} />
          </Form.Item>

          <Form.Item label={t('systemSettings.defaultValue')} name="defaultValue">
            <Input placeholder={t('systemSettings.defaultValuePlaceholder')} />
          </Form.Item>

          <Form.Item label={t('systemSettings.sortOrder')} name="sortOrder" initialValue={0}>
            <InputNumber style={{ width: "100%" }} min={0} />
          </Form.Item>

          <Form.Item name="isSensitive" valuePropName="checked" initialValue={false}>
            <Switch /> {t('systemSettings.isSensitive')}
          </Form.Item>

          <Form.Item name="isReadOnly" valuePropName="checked" initialValue={false}>
            <Switch /> {t('systemSettings.isReadOnly')}
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

