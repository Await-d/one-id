import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Button,
  Card,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
  type FormInstance,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import { PlusOutlined, CodeOutlined } from "@ant-design/icons";
import { clientsApi } from "../lib/clientsApi";
import { IntegrationCodeModal } from "../components/IntegrationCodeModal";
import type {
  ClientSummary,
  CreateClientPayload,
  UpdateClientPayload,
  UpdateClientScopesPayload,
} from "../types/clients";

type CreateFormValues = CreateClientPayload & { scopes: string[] };
type UpdateFormValues = UpdateClientPayload & { scopes: string[] };

const DEFAULT_SCOPES = ["openid", "profile"];

export function ClientsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<ClientSummary | null>(null);
  const [scopesClient, setScopesClient] = useState<ClientSummary | null>(null);
  const [integrationClient, setIntegrationClient] = useState<ClientSummary | null>(null);

  const [createForm] = Form.useForm<CreateFormValues>();
  const [editForm] = Form.useForm<UpdateFormValues>();
  const [scopesForm] = Form.useForm<UpdateClientScopesPayload>();

  const { data, isLoading, error } = useQuery({
    queryKey: ["clients"],
    queryFn: () => clientsApi.list(),
  });

  // 确保 data 始终是数组
  const clients = Array.isArray(data) ? data : [];

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["clients"] });

  const createMutation = useMutation({
    mutationFn: clientsApi.create,
    onSuccess: () => {
      message.success(t("clients.createSuccess"));
      setCreateOpen(false);
      createForm.resetFields();
      invalidate();
    },
    onError: (error: any) => {
      handleValidationError(error, createForm);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ clientId, payload }: { clientId: string; payload: UpdateClientPayload }) =>
      clientsApi.update(clientId, payload),
    onSuccess: () => {
      message.success(t("clients.updateSuccess"));
      setEditingClient(null);
      invalidate();
    },
    onError: (error: any) => {
      handleValidationError(error, editForm);
    },
  });

  const scopesMutation = useMutation({
    mutationFn: ({ clientId, payload }: { clientId: string; payload: UpdateClientScopesPayload }) =>
      clientsApi.updateScopes(clientId, payload),
    onSuccess: () => {
      message.success(t("clients.scopesUpdateSuccess"));
      setScopesClient(null);
      invalidate();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: clientsApi.remove,
    onSuccess: () => {
      message.success(t("clients.deleteSuccess"));
      invalidate();
    },
  });

  const createClientType = Form.useWatch("clientType", createForm) ?? "public";
  const editClientType = Form.useWatch("clientType", editForm) ?? editingClient?.clientType ?? "public";

  const columns: ColumnsType<ClientSummary> = useMemo(
    () => [
      {
        title: t("clients.clientId"),
        dataIndex: "clientId",
        key: "clientId",
        width: 200,
      },
      {
        title: t("clients.displayName"),
        dataIndex: "displayName",
        key: "displayName",
        width: 220,
      },
      {
        title: t("clients.clientType"),
        dataIndex: "clientType",
        key: "clientType",
        width: 120,
        render: (value: string) => <Tag color="blue">{value}</Tag>,
      },
      {
        title: t("clients.callbackAddress"),
        dataIndex: "redirectUris",
        key: "redirectUris",
        render: (uris: string[]) =>
          uris.length ? (
            <ul className="space-y-1">
              {uris.map((uri) => (
                <li key={uri} className="text-xs text-gray-500">
                  {uri}
                </li>
              ))}
            </ul>
          ) : (
            <Typography.Text type="secondary">{t("clients.notConfigured")}</Typography.Text>
          ),
      },
      {
        title: t("clients.logoutCallback"),
        dataIndex: "postLogoutRedirectUris",
        key: "postLogoutRedirectUris",
        render: (uris: string[]) =>
          uris.length ? (
            <ul className="space-y-1">
              {uris.map((uri) => (
                <li key={uri} className="text-xs text-gray-500">
                  {uri}
                </li>
              ))}
            </ul>
          ) : (
            <Typography.Text type="secondary">{t("clients.notConfigured")}</Typography.Text>
          ),
      },
      {
        title: t("clients.scopes"),
        dataIndex: "scopes",
        key: "scopes",
        render: (scopes: string[]) =>
          scopes.length
            ? scopes.map((scope) => <Tag key={scope}>{scope}</Tag>)
            : <Typography.Text type="secondary">{t("clients.default")}</Typography.Text>,
      },
      {
        title: t("clients.actions"),
        key: "actions",
        width: 280,
        render: (_, record) => (
          <Space size="small" wrap>
            <Button
              type="link"
              icon={<CodeOutlined />}
              onClick={() => setIntegrationClient(record)}
            >
              {t("clients.integration")}
            </Button>
            <Button type="link" onClick={() => handleOpenEdit(record)}>
              {t("clients.edit")}
            </Button>
            <Button type="link" onClick={() => handleOpenScopes(record)}>
              {t("clients.scopes")}
            </Button>
            <Popconfirm
              title={t("clients.confirmDeleteTitle")}
              okButtonProps={{ loading: deleteMutation.isPending }}
              onConfirm={() => deleteMutation.mutate(record.clientId)}
            >
              <Button type="link" danger>
                {t("clients.delete")}
              </Button>
            </Popconfirm>
          </Space>
        ),
      },
    ],
    [deleteMutation.isPending, t],
  );

  const handleOpenCreate = () => {
    createForm.resetFields();
    createForm.setFieldsValue({ scopes: DEFAULT_SCOPES, clientType: "public" });
    setCreateOpen(true);
  };

  const handleOpenEdit = (client: ClientSummary) => {
    setEditingClient(client);
    editForm.setFieldsValue({
      displayName: client.displayName,
      redirectUri: client.redirectUris[0] ?? "",
      postLogoutRedirectUri: client.postLogoutRedirectUris[0],
      scopes: client.scopes.length ? client.scopes : DEFAULT_SCOPES,
      clientType: client.clientType,
    });
  };

  const handleValidationError = (error: any, form: FormInstance) => {
    const detail = error?.response?.data;
    if (detail?.errors) {
      const fieldMap: Record<string, string> = {
        RedirectUri: "redirectUri",
        PostLogoutRedirectUri: "postLogoutRedirectUri",
        ClientId: "clientId",
        DisplayName: "displayName",
        ClientSecret: "clientSecret",
      };

      const items = Object.entries(detail.errors).map(([field, messages]) => ({
        name: fieldMap[field] ?? field,
        errors: Array.isArray(messages) ? messages : [String(messages)],
      }));
      form.setFields(items as any);

      const firstMessage = items[0]?.errors?.[0];
      if (firstMessage) {
        message.error(firstMessage);
        return;
      }
    }

    message.error(detail?.title || detail?.detail || t("clients.requestFailed"));
  };

  const handleOpenScopes = (client: ClientSummary) => {
    setScopesClient(client);
    scopesForm.setFieldsValue({ scopes: client.scopes.length ? client.scopes : DEFAULT_SCOPES });
  };

  const handleCreateSubmit = async () => {
    const values = await createForm.validateFields();
    createMutation.mutate({
      clientId: values.clientId,
      displayName: values.displayName,
      redirectUri: values.redirectUri,
      postLogoutRedirectUri: values.postLogoutRedirectUri,
      scopes: values.scopes?.length ? values.scopes : undefined,
      clientType: values.clientType,
      clientSecret: values.clientType === "confidential" ? values.clientSecret : undefined,
    });
  };

  const handleEditSubmit = async () => {
    if (!editingClient) return;
    const values = await editForm.validateFields();
    updateMutation.mutate({
      clientId: editingClient.clientId,
      payload: {
        displayName: values.displayName,
        redirectUri: values.redirectUri,
        postLogoutRedirectUri: values.postLogoutRedirectUri,
        scopes: values.scopes?.length ? values.scopes : DEFAULT_SCOPES,
        clientType: values.clientType,
        clientSecret: values.clientType === "confidential" ? values.clientSecret : undefined,
      },
    });
  };

  const handleScopesSubmit = async () => {
    if (!scopesClient) return;
    const values = await scopesForm.validateFields();
    scopesMutation.mutate({
      clientId: scopesClient.clientId,
      payload: {
        scopes: values.scopes,
      },
    });
  };

  useEffect(() => {
    if (createClientType !== "confidential") {
      createForm.setFieldValue("clientSecret", undefined);
    }
  }, [createClientType, createForm]);

  useEffect(() => {
    if (editClientType !== "confidential") {
      editForm.setFieldValue("clientSecret", undefined);
    }
  }, [editClientType, editForm]);

  // 显示错误提示
  useEffect(() => {
    if (error) {
      message.error(`${t("clients.loadError")}: ${error instanceof Error ? error.message : t("clients.unknownError")}`);
    }
  }, [error, t]);

  return (
    <div style={{ padding: "24px", background: "linear-gradient(to bottom right, #f0f9ff, #ffffff, #faf5ff)", minHeight: "100vh" }}>
      <Card
        title={
          <div>
            <div style={{ fontSize: "20px", fontWeight: 600, background: "linear-gradient(to right, #2563eb, #7c3aed)", WebkitBackgroundClip: "text", WebkitTextFillColor: "transparent" }}>
              {t("clients.title")}
            </div>
            <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px", fontWeight: 400 }}>{t("clients.subtitle")}</p>
          </div>
        }
        bordered={false}
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleOpenCreate}
            style={{
              borderRadius: "8px",
              height: "40px",
              fontSize: "14px",
              fontWeight: 500,
              background: "linear-gradient(to right, #2563eb, #7c3aed)",
              border: "none",
              boxShadow: "0 4px 12px rgba(37, 99, 235, 0.3)"
            }}
          >
            {t("clients.addClient")}
          </Button>
        }
        style={{
          borderRadius: "16px",
          boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
          border: "1px solid #e5e7eb"
        }}
      >
        <Table
          rowKey={(record) => record.clientId}
          loading={isLoading}
          dataSource={clients}
          columns={columns}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => t("auditLogs.totalRecords", { total }),
            style: { marginTop: "16px" }
          }}
          style={{
            borderRadius: "12px",
            overflow: "hidden"
          }}
        />

        <Modal
          title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("clients.createClient")}</span>}
          open={isCreateOpen}
          onCancel={() => setCreateOpen(false)}
          onOk={handleCreateSubmit}
          confirmLoading={createMutation.isPending}
          styles={{
            header: { borderBottom: "1px solid #e5e7eb", paddingBottom: "16px" },
            body: { paddingTop: "24px" }
          }}
          okButtonProps={{
            style: {
              background: "linear-gradient(to right, #2563eb, #7c3aed)",
              border: "none",
              borderRadius: "8px"
            }
          }}
          cancelButtonProps={{
            style: {
              borderRadius: "8px"
            }
          }}
        >
          <Form form={createForm} layout="vertical">
            <Form.Item
              name="clientId"
              label={t("clients.clientId")}
              rules={[{ required: true, message: t("clients.clientIdRequired") }]}
            >
              <Input placeholder={t("clients.clientIdPlaceholder")} />
            </Form.Item>
            <Form.Item
              name="displayName"
              label={t("clients.displayName")}
              rules={[{ required: true, message: t("clients.displayNameRequired") }]}
            >
              <Input placeholder={t("clients.displayNamePlaceholder")} />
            </Form.Item>
            <Form.Item
              name="redirectUri"
              label={t("clients.callbackAddress")}
              rules={[{ required: true, message: t("clients.redirectUrisRequired") }]}
            >
              <Input placeholder={t("clients.redirectUrisPlaceholder")} />
            </Form.Item>
            <Form.Item name="postLogoutRedirectUri" label={t("clients.logoutCallback")}>
              <Input placeholder={t("clients.postLogoutUrisPlaceholder")} />
            </Form.Item>
            <Form.Item name="scopes" label={t("clients.scopes")} initialValue={DEFAULT_SCOPES}>
              <Select
                mode="tags"
                placeholder={t("clients.scopesPlaceholder")}
                options={DEFAULT_SCOPES.map((scope) => ({ value: scope, label: scope }))}
              />
            </Form.Item>
            <Form.Item
              name="clientType"
              label={t("clients.clientType")}
              initialValue="public"
              rules={[{ required: true, message: t("clients.clientTypePlaceholder") }]}
            >
              <Select
                options={[
                  { label: t("clients.public"), value: "public" },
                  { label: t("clients.confidential"), value: "confidential" },
                ]}
              />
            </Form.Item>
            {createClientType === "confidential" && (
              <Form.Item
                name="clientSecret"
                label={t("clients.clientSecret")}
                rules={[{ required: true, message: t("clients.clientSecretRequired") }]}
              >
                <Input.Password placeholder={t("clients.clientSecretPlaceholder")} />
              </Form.Item>
            )}
          </Form>
        </Modal>

        <Modal
          title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("clients.editClient")}：{editingClient?.clientId ?? ""}</span>}
          open={Boolean(editingClient)}
          onCancel={() => setEditingClient(null)}
          onOk={handleEditSubmit}
          confirmLoading={updateMutation.isPending}
          destroyOnClose
          styles={{
            header: { borderBottom: "1px solid #e5e7eb", paddingBottom: "16px" },
            body: { paddingTop: "24px" }
          }}
          okButtonProps={{
            style: {
              background: "linear-gradient(to right, #2563eb, #7c3aed)",
              border: "none",
              borderRadius: "8px"
            }
          }}
          cancelButtonProps={{
            style: {
              borderRadius: "8px"
            }
          }}
        >
          <Form form={editForm} layout="vertical" preserve={false}>
            <Form.Item label={t("clients.clientId")}>
              <Input value={editingClient?.clientId} disabled />
            </Form.Item>
            <Form.Item
              name="displayName"
              label={t("clients.displayName")}
              rules={[{ required: true, message: t("clients.displayNameRequired") }]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              name="redirectUri"
              label={t("clients.callbackAddress")}
              rules={[{ required: true, message: t("clients.redirectUrisRequired") }]}
            >
              <Input />
            </Form.Item>
            <Form.Item name="postLogoutRedirectUri" label={t("clients.logoutCallback")}>
              <Input />
            </Form.Item>
            <Form.Item name="scopes" label={t("clients.scopes")}>
              <Select
                mode="tags"
                placeholder={t("clients.scopesPlaceholder")}
                options={(editingClient?.scopes?.length ? editingClient.scopes : DEFAULT_SCOPES).map((scope) => ({
                  value: scope,
                  label: scope,
                }))}
              />
            </Form.Item>
            <Form.Item
              name="clientType"
              label={t("clients.clientType")}
              rules={[{ required: true, message: t("clients.clientTypePlaceholder") }]}
            >
              <Select
                options={[
                  { label: t("clients.public"), value: "public" },
                  { label: t("clients.confidential"), value: "confidential" },
                ]}
              />
            </Form.Item>
            {editClientType === "confidential" && (
              <Form.Item
                name="clientSecret"
                label={t("clients.clientSecret")}
                rules={[{ required: true, message: t("clients.clientSecretRequired") }]}
              >
                <Input.Password placeholder={t("clients.clientSecretPlaceholder")} />
              </Form.Item>
            )}
          </Form>
        </Modal>

        <Modal
          title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("clients.updateScopes")}：{scopesClient?.clientId ?? ""}</span>}
          open={Boolean(scopesClient)}
          onCancel={() => setScopesClient(null)}
          onOk={handleScopesSubmit}
          confirmLoading={scopesMutation.isPending}
          destroyOnClose
          styles={{
            header: { borderBottom: "1px solid #e5e7eb", paddingBottom: "16px" },
            body: { paddingTop: "24px" }
          }}
          okButtonProps={{
            style: {
              background: "linear-gradient(to right, #2563eb, #7c3aed)",
              border: "none",
              borderRadius: "8px"
            }
          }}
          cancelButtonProps={{
            style: {
              borderRadius: "8px"
            }
          }}
        >
          <Form form={scopesForm} layout="vertical" preserve={false}>
            <Form.Item
              name="scopes"
              label={t("clients.scopes")}
              rules={[{ required: true, message: t("clients.scopesPlaceholder") }]}
            >
              <Select
                mode="tags"
                placeholder={t("clients.scopesPlaceholder")}
                options={(scopesClient?.scopes?.length ? scopesClient.scopes : DEFAULT_SCOPES).map((scope) => ({
                  value: scope,
                  label: scope,
                }))}
              />
            </Form.Item>
          </Form>
        </Modal>

        <IntegrationCodeModal
          client={integrationClient}
          open={Boolean(integrationClient)}
          onClose={() => setIntegrationClient(null)}
        />
      </Card>
    </div>
  );
}
