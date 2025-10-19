import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Button,
  Card,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
  message,
  InputNumber,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import { PlusOutlined, CheckCircleOutlined, StopOutlined, LoginOutlined } from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { externalAuthApi } from "../lib/externalAuthApi";
import { userManager } from "../lib/oidcConfig";
import type {
  ExternalAuthProvider,
  CreateExternalAuthProviderPayload,
  UpdateExternalAuthProviderPayload,
} from "../types/externalAuth";

const { Title, Text } = Typography;

const PROVIDER_TYPE_OPTIONS = [
  { value: "GitHub", label: "GitHub" },
  { value: "Google", label: "Google" },
  { value: "Gitee", label: "Gitee (码云)" },
  { value: "Microsoft", label: "Microsoft" },
  { value: "Apple", label: "Apple" },
  { value: "Facebook", label: "Facebook" },
  { value: "LinkedIn", label: "LinkedIn" },
  { value: "WeChat", label: "WeChat (微信)" },
  { value: "QQ", label: "QQ" },
];

type AdditionalConfigPair = { key?: string; value?: string };

const mapRecordToPairs = (record?: Record<string, string>): AdditionalConfigPair[] => {
  if (!record) {
    return [];
  }

  return Object.entries(record).map(([key, value]) => ({ key, value }));
};

const mapPairsToRecord = (
  pairs?: AdditionalConfigPair[]
): Record<string, string> | undefined => {
  if (!pairs) {
    return undefined;
  }

  const result: Record<string, string> = {};
  pairs.forEach(({ key, value }) => {
    const trimmedKey = key?.trim();
    const trimmedValue = value?.trim();
    if (trimmedKey && trimmedValue) {
      result[trimmedKey] = trimmedValue;
    }
  });

  return Object.keys(result).length > 0 ? result : undefined;
};

const normalizeScopes = (scopes?: string[]): string[] | undefined => {
  if (!scopes) {
    return undefined;
  }

  const cleaned = Array.from(
    new Set(
      scopes
        .map((scope) => scope.trim())
        .filter((scope) => scope.length > 0)
    )
  );

  return cleaned.length > 0 ? cleaned : undefined;
};

type CreateFormValues = Omit<CreateExternalAuthProviderPayload, "additionalConfig"> & {
  additionalConfig?: AdditionalConfigPair[];
};

type UpdateFormValues = Omit<UpdateExternalAuthProviderPayload, "additionalConfig"> & {
  additionalConfig?: AdditionalConfigPair[];
};

export function ExternalAuthProvidersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const [editingProvider, setEditingProvider] = useState<ExternalAuthProvider | null>(null);

  const [createForm] = Form.useForm<CreateFormValues>();
  const [editForm] = Form.useForm<UpdateFormValues>();

  // 测试登录功能 - 使用OIDC配置中的Authority
  const handleTestLogin = (providerName: string) => {
    // 从 userManager 的设置中获取 Authority URL（避免硬编码）
    const authority = userManager.settings.authority;
    const testUrl = `${authority}/api/externalauth/challenge/${providerName}?returnUrl=/`;

    // 打开新窗口进行测试登录
    const width = 600;
    const height = 700;
    const left = window.screenX + (window.outerWidth - width) / 2;
    const top = window.screenY + (window.outerHeight - height) / 2;

    window.open(
      testUrl,
      `test-login-${providerName}`,
      `width=${width},height=${height},left=${left},top=${top},toolbar=no,menubar=no,location=no`
    );
  };

  const { data, isLoading } = useQuery({
    queryKey: ["externalAuthProviders"],
    queryFn: () => externalAuthApi.list(),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["externalAuthProviders"] });

  const createMutation = useMutation({
    mutationFn: externalAuthApi.create,
    onSuccess: () => {
      message.success(t("externalAuth.createSuccess"));
      setCreateOpen(false);
      createForm.resetFields();
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("externalAuth.createFailed"));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateExternalAuthProviderPayload }) =>
      externalAuthApi.update(id, payload),
    onSuccess: () => {
      message.success(t("externalAuth.updateSuccess"));
      setEditingProvider(null);
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("externalAuth.updateFailed"));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: externalAuthApi.remove,
    onSuccess: () => {
      message.success(t("externalAuth.deleteSuccess"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("externalAuth.deleteFailed"));
    },
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      externalAuthApi.toggle(id, enabled),
    onSuccess: () => {
      message.success(t("externalAuth.statusUpdated"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("externalAuth.operationFailed"));
    },
  });

  const columns: ColumnsType<ExternalAuthProvider> = useMemo(
    () => [
      {
        title: t("externalAuth.providerType"),
        dataIndex: "providerType",
        key: "providerType",
        width: 100,
        render: (type: string) => <Tag color="blue">{type}</Tag>,
      },
      {
        title: t("externalAuth.name"),
        dataIndex: "name",
        key: "name",
        width: 150,
        render: (name: string) => <Tag color="cyan">{name}</Tag>,
      },
      {
        title: t("externalAuth.displayName"),
        dataIndex: "displayName",
        key: "displayName",
        width: 150,
      },
      {
        title: t("common.status"),
        dataIndex: "enabled",
        key: "enabled",
        width: 100,
        render: (enabled: boolean) =>
          enabled ? (
            <Tag icon={<CheckCircleOutlined />} color="success">
              {t("common.enabled")}
            </Tag>
          ) : (
            <Tag icon={<StopOutlined />} color="default">
              {t("common.disabled")}
            </Tag>
          ),
      },
      {
        title: t("externalAuth.clientId"),
        dataIndex: "clientId",
        key: "clientId",
        width: 200,
        ellipsis: true,
      },
      {
        title: t("externalAuth.callbackPath"),
        dataIndex: "callbackPath",
        key: "callbackPath",
        width: 150,
      },
      {
        title: t("externalAuth.scopes"),
        dataIndex: "scopes",
        key: "scopes",
        width: 200,
        render: (scopes: string[]) => (
          <Space wrap>
            {scopes.map((scope) => (
              <Tag key={scope}>{scope}</Tag>
            ))}
          </Space>
        ),
      },
      {
        title: t("externalAuth.additionalConfig"),
        dataIndex: "additionalConfig",
        key: "additionalConfig",
        width: 220,
        render: (config?: Record<string, string>) => {
          const entries = Object.entries(config || {});
          if (entries.length === 0) {
            return <Text type="secondary">{t("externalAuth.none")}</Text>;
          }

          return (
            <Space wrap>
              {entries.map(([key, value]) => (
                <Tag key={key} color="geekblue">
                  {key}: {value}
                </Tag>
              ))}
            </Space>
          );
        },
      },
      {
        title: t("externalAuth.displayOrder"),
        dataIndex: "displayOrder",
        key: "displayOrder",
        width: 80,
      },
      {
        title: t("common.actions"),
        key: "action",
        fixed: "right",
        width: 330,
        render: (_, record: ExternalAuthProvider) => (
          <Space size="small">
            <Button
              type="primary"
              size="small"
              icon={<LoginOutlined />}
              disabled={!record.enabled}
              onClick={() => handleTestLogin(record.name)}
            >
              {t("externalAuth.testLogin")}
            </Button>
            <Button
              type="link"
              size="small"
              onClick={() => {
                setEditingProvider(record);
                editForm.setFieldsValue({
                  displayName: record.displayName,
                  clientId: record.clientId,
                  callbackPath: record.callbackPath,
                  enabled: record.enabled,
                  scopes: record.scopes,
                  displayOrder: record.displayOrder,
                  additionalConfig: mapRecordToPairs(record.additionalConfig),
                });
              }}
            >
              {t("common.edit")}
            </Button>
            <Button
              type="link"
              size="small"
              onClick={() => toggleMutation.mutate({ id: record.id, enabled: !record.enabled })}
            >
              {record.enabled ? t("externalAuth.disable") : t("externalAuth.enable")}
            </Button>
            <Popconfirm
              title={t("externalAuth.confirmDelete")}
              onConfirm={() => deleteMutation.mutate(record.id)}
              okText={t("common.confirm")}
              cancelText={t("common.cancel")}
            >
              <Button type="link" size="small" danger>
                {t("common.delete")}
              </Button>
            </Popconfirm>
          </Space>
        ),
      },
    ],
    [deleteMutation, toggleMutation, editForm, t, handleTestLogin]
  );

  return (
    <div style={{ padding: "24px" }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: "100%" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <Title level={4} style={{ margin: 0 }}>
                {t("externalAuth.title")}
              </Title>
              <Text type="secondary">{t("externalAuth.subtitle")}</Text>
            </div>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
              {t("externalAuth.addProvider")}
            </Button>
          </div>

          <Table
            columns={columns}
            dataSource={data || []}
            loading={isLoading}
            rowKey="id"
            scroll={{ x: 1500 }}
            pagination={{
              pageSize: 10,
              showSizeChanger: true,
              showTotal: (total) => t("auditLogs.totalRecords", { total }),
            }}
          />
        </Space>
      </Card>

      {/* 创建提供者模态框 */}
      <Modal
        title={t("externalAuth.createProvider")}
        open={isCreateOpen}
        onCancel={() => {
          setCreateOpen(false);
          createForm.resetFields();
        }}
        onOk={() => createForm.submit()}
        confirmLoading={createMutation.isPending}
        width={600}
      >
        <Form
          form={createForm}
          layout="vertical"
          initialValues={{ displayOrder: 0 }}
          onFinish={(values) => {
            const { additionalConfig, scopes, displayOrder, ...rest } = values;
            const payload: CreateExternalAuthProviderPayload = {
              ...rest,
              displayOrder: displayOrder ?? 0,
              scopes: normalizeScopes(scopes),
              additionalConfig: mapPairsToRecord(additionalConfig),
            };

            createMutation.mutate(payload);
          }}
        >
          <Form.Item
            name="providerType"
            label={t("externalAuth.providerType")}
            rules={[{ required: true, message: t("externalAuth.providerTypeRequired") }]}
          >
            <Select options={PROVIDER_TYPE_OPTIONS} placeholder={t("externalAuth.providerTypePlaceholder")} />
          </Form.Item>
          <Form.Item
            name="name"
            label={t("externalAuth.name")}
            tooltip={t("externalAuth.nameTooltip")}
            rules={[{ required: true, message: t("externalAuth.nameRequired") }]}
          >
            <Input placeholder={t("externalAuth.namePlaceholder")} />
          </Form.Item>
          <Form.Item
            name="displayName"
            label={t("externalAuth.displayName")}
            rules={[{ required: true, message: t("externalAuth.displayNameRequired") }]}
          >
            <Input placeholder={t("externalAuth.displayNamePlaceholder")} />
          </Form.Item>
          <Form.Item
            name="clientId"
            label={t("externalAuth.clientId")}
            rules={[{ required: true, message: t("externalAuth.clientIdRequired") }]}
          >
            <Input placeholder={t("externalAuth.clientIdPlaceholder")} />
          </Form.Item>
          <Form.Item
            name="clientSecret"
            label={t("externalAuth.clientSecret")}
            rules={[{ required: true, message: t("externalAuth.clientSecretRequired") }]}
          >
            <Input.Password placeholder={t("externalAuth.clientSecretPlaceholder")} />
          </Form.Item>
          <Form.Item name="callbackPath" label={t("externalAuth.callbackPath")}>
            <Input placeholder={t("externalAuth.callbackPathPlaceholder")} />
          </Form.Item>
          <Form.Item name="scopes" label={t("externalAuth.scopes")}>
            <Select mode="tags" placeholder={t("externalAuth.scopesPlaceholder")} />
          </Form.Item>
          <Form.List name="additionalConfig">
            {(fields, { add, remove }) => (
              <div>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <Text>{t("externalAuth.additionalConfigOptional")}</Text>
                  <Button type="link" onClick={() => add()}>
                    {t("externalAuth.addKeyValue")}
                  </Button>
                </div>
                {fields.length === 0 && (
                  <Text type="secondary">{t("externalAuth.additionalConfigHint")}</Text>
                )}
                {fields.map((field) => (
                  <Space key={field.key} align="baseline" style={{ display: "flex", marginBottom: 8 }}>
                    <Form.Item
                      {...field}
                      name={[field.name, "key"]}
                      fieldKey={field.fieldKey !== undefined ? [field.fieldKey, "key"] : undefined}
                      rules={[{ required: true, message: t("externalAuth.keyRequired") }]}
                    >
                      <Input placeholder={t("externalAuth.keyPlaceholder")} />
                    </Form.Item>
                    <Form.Item
                      {...field}
                      name={[field.name, "value"]}
                      fieldKey={field.fieldKey !== undefined ? [field.fieldKey, "value"] : undefined}
                      rules={[{ required: true, message: t("externalAuth.valueRequired") }]}
                    >
                      <Input placeholder={t("externalAuth.valuePlaceholder")} />
                    </Form.Item>
                    <Button type="link" onClick={() => remove(field.name)}>
                      {t("externalAuth.remove")}
                    </Button>
                  </Space>
                ))}
              </div>
            )}
          </Form.List>
          <Form.Item name="displayOrder" label={t("externalAuth.displayOrder")}>
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
        </Form>
      </Modal>

      {/* 编辑提供者模态框 */}
      <Modal
        title={t("externalAuth.editProvider")}
        open={!!editingProvider}
        onCancel={() => setEditingProvider(null)}
        onOk={() => editForm.submit()}
        confirmLoading={updateMutation.isPending}
        width={600}
      >
        {editingProvider && (
          <Form
            form={editForm}
            layout="vertical"
            onFinish={(values) => {
              const { additionalConfig, scopes, ...rest } = values;
              const payload: UpdateExternalAuthProviderPayload = {
                ...rest,
                scopes: normalizeScopes(scopes),
                additionalConfig: mapPairsToRecord(additionalConfig),
              };

              updateMutation.mutate({ id: editingProvider.id, payload });
            }}
          >
            <Form.Item label={t("externalAuth.providerType")}>
              <Input value={editingProvider.providerType} disabled />
            </Form.Item>
            <Form.Item label={t("externalAuth.name")}>
              <Input value={editingProvider.name} disabled />
            </Form.Item>
            <Form.Item name="displayName" label={t("externalAuth.displayName")}>
              <Input placeholder={t("externalAuth.displayNamePlaceholder")} />
            </Form.Item>
            <Form.Item name="clientId" label={t("externalAuth.clientId")}>
              <Input placeholder={t("externalAuth.clientIdPlaceholder")} />
            </Form.Item>
            <Form.Item name="clientSecret" label={t("externalAuth.clientSecret")}>
              <Input.Password placeholder={t("externalAuth.clientSecretEditPlaceholder")} />
            </Form.Item>
            <Form.Item name="callbackPath" label={t("externalAuth.callbackPath")}>
              <Input placeholder={t("externalAuth.callbackPathEditPlaceholder")} />
            </Form.Item>
            <Form.Item name="enabled" label={t("externalAuth.enabledStatus")} valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item name="scopes" label={t("externalAuth.scopes")}>
              <Select mode="tags" placeholder={t("externalAuth.scopesPlaceholder")} />
            </Form.Item>
            <Form.List name="additionalConfig">
              {(fields, { add, remove }) => (
                <div>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <Text>{t("externalAuth.additionalConfigOptional")}</Text>
                    <Button type="link" onClick={() => add()}>
                      {t("externalAuth.addKeyValue")}
                    </Button>
                  </div>
                  {fields.length === 0 && (
                    <Text type="secondary">{t("externalAuth.noAdditionalConfig")}</Text>
                  )}
                  {fields.map((field) => (
                    <Space key={field.key} align="baseline" style={{ display: "flex", marginBottom: 8 }}>
                      <Form.Item
                        {...field}
                        name={[field.name, "key"]}
                        fieldKey={field.fieldKey !== undefined ? [field.fieldKey, "key"] : undefined}
                        rules={[{ required: true, message: t("externalAuth.keyRequired") }]}
                      >
                        <Input placeholder={t("externalAuth.keyEditPlaceholder")} />
                      </Form.Item>
                      <Form.Item
                        {...field}
                        name={[field.name, "value"]}
                        fieldKey={field.fieldKey !== undefined ? [field.fieldKey, "value"] : undefined}
                        rules={[{ required: true, message: t("externalAuth.valueRequired") }]}
                      >
                        <Input placeholder={t("externalAuth.valueEditPlaceholder")} />
                      </Form.Item>
                      <Button type="link" onClick={() => remove(field.name)}>
                        {t("externalAuth.remove")}
                      </Button>
                    </Space>
                  ))}
                </div>
              )}
            </Form.List>
            <Form.Item name="displayOrder" label={t("externalAuth.displayOrder")}>
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  );
}
