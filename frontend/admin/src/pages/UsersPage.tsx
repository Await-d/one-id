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
  Space,
  Switch,
  Table,
  Tag,
  Typography,
  message,
  Descriptions,
  Badge,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import { PlusOutlined, LockOutlined, UnlockOutlined, KeyOutlined, MobileOutlined, ClockCircleOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { usersApi } from "../lib/usersApi";
import type {
  UserSummary,
  CreateUserPayload,
  UpdateUserPayload,
  ChangePasswordPayload,
} from "../types/users";

const { Title } = Typography;

export function UsersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null);
  const [viewingUser, setViewingUser] = useState<UserSummary | null>(null);
  const [passwordUser, setPasswordUser] = useState<UserSummary | null>(null);

  const [createForm] = Form.useForm<CreateUserPayload>();
  const [editForm] = Form.useForm<UpdateUserPayload>();
  const [passwordForm] = Form.useForm<ChangePasswordPayload>();

  const { data, isLoading } = useQuery({
    queryKey: ["users"],
    queryFn: () => usersApi.list(),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["users"] });

  const createMutation = useMutation({
    mutationFn: usersApi.create,
    onSuccess: () => {
      message.success(t("users.createSuccess"));
      setCreateOpen(false);
      createForm.resetFields();
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("users.createFailed"));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ userId, payload }: { userId: string; payload: UpdateUserPayload }) =>
      usersApi.update(userId, payload),
    onSuccess: () => {
      message.success(t("users.updateSuccess"));
      setEditingUser(null);
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("users.updateFailed"));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: usersApi.remove,
    onSuccess: () => {
      message.success(t("users.deleteSuccess"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("users.deleteFailed"));
    },
  });

  const passwordMutation = useMutation({
    mutationFn: ({ userId, payload }: { userId: string; payload: ChangePasswordPayload }) =>
      usersApi.changePassword(userId, payload),
    onSuccess: () => {
      message.success(t("users.passwordChangeSuccess"));
      setPasswordUser(null);
      passwordForm.resetFields();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("users.passwordChangeFailed"));
    },
  });

  const unlockMutation = useMutation({
    mutationFn: usersApi.unlock,
    onSuccess: () => {
      message.success(t("users.unlockSuccess"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.error || t("users.unlockFailed"));
    },
  });

  const columns: ColumnsType<UserSummary> = useMemo(
    () => [
      {
        title: t("users.username"),
        dataIndex: "userName",
        key: "userName",
        width: 200,
      },
      {
        title: t("users.email"),
        dataIndex: "email",
        key: "email",
        width: 250,
        render: (email: string, record: UserSummary) => (
          <Space>
            {email}
            {record.emailConfirmed && <Badge status="success" text={t("users.emailConfirmed")} />}
          </Space>
        ),
      },
      {
        title: t("users.displayName"),
        dataIndex: "displayName",
        key: "displayName",
        width: 150,
        render: (name?: string) => name || "-",
      },
      {
        title: t("users.type"),
        key: "type",
        width: 100,
        render: (_, record: UserSummary) => (
          <Tag color={record.isExternal ? "blue" : "default"}>
            {record.isExternal ? t("users.external") : t("users.local")}
          </Tag>
        ),
      },
      {
        title: t("users.status"),
        key: "status",
        width: 120,
        render: (_, record: UserSummary) => {
          const isLocked = record.lockoutEnd && new Date(record.lockoutEnd) > new Date();
          return (
            <Space>
              {isLocked ? (
                <Tag color="red">{t("users.locked")}</Tag>
              ) : (
                <Tag color="green">{t("users.normal")}</Tag>
              )}
              {record.accessFailedCount > 0 && (
                <Tag color="warning">{t("users.accessFailedCount", { count: record.accessFailedCount })}</Tag>
              )}
            </Space>
          );
        },
      },
      {
        title: t("users.roles"),
        dataIndex: "roles",
        key: "roles",
        width: 150,
        render: (roles: string[]) => (
          <Space wrap>
            {roles.length > 0 ? roles.map((role) => <Tag key={role}>{role}</Tag>) : "-"}
          </Space>
        ),
      },
      {
        title: t("users.linkedAccounts"),
        dataIndex: "externalLogins",
        key: "externalLogins",
        width: 150,
        render: (logins: any[]) => logins.length,
      },
      {
        title: t("users.actions"),
        key: "action",
        fixed: "right",
        width: 420,
        render: (_, record: UserSummary) => {
          const isLocked = record.lockoutEnd && new Date(record.lockoutEnd) > new Date();
          return (
            <Space size="small">
              <Button
                type="link"
                size="small"
                onClick={() => setViewingUser(record)}
              >
                {t("common.details")}
              </Button>
              <Button
                type="link"
                size="small"
                onClick={() => {
                  setEditingUser(record);
                  editForm.setFieldsValue({
                    displayName: record.displayName,
                    emailConfirmed: record.emailConfirmed,
                    lockoutEnabled: record.lockoutEnabled,
                  });
                }}
              >
                {t("users.edit")}
              </Button>
              <Button
                type="link"
                size="small"
                icon={<KeyOutlined />}
                onClick={() => setPasswordUser(record)}
              >
                {t("users.changePassword")}
              </Button>
              <Button
                type="link"
                size="small"
                icon={<MobileOutlined />}
                onClick={() => navigate(`/users/${record.id}/devices`)}
              >
                设备
              </Button>
              <Button
                type="link"
                size="small"
                icon={<ClockCircleOutlined />}
                onClick={() => navigate(`/users/${record.id}/sessions`)}
              >
                会话
              </Button>
              {isLocked && (
                <Button
                  type="link"
                  size="small"
                  icon={<UnlockOutlined />}
                  onClick={() => unlockMutation.mutate(record.id)}
                >
                  {t("users.unlock")}
                </Button>
              )}
              <Popconfirm
                title={t("users.confirmDelete", { username: record.userName })}
                onConfirm={() => deleteMutation.mutate(record.id)}
                okText={t("common.confirm")}
                cancelText={t("common.cancel")}
              >
                <Button type="link" size="small" danger>
                  {t("users.delete")}
                </Button>
              </Popconfirm>
            </Space>
          );
        },
      },
    ],
    [deleteMutation, unlockMutation, editForm, t],
  );

  return (
    <div style={{ padding: "24px", background: "linear-gradient(to bottom right, #f0f9ff, #ffffff, #faf5ff)", minHeight: "100vh" }}>
      <Card
        style={{
          borderRadius: "16px",
          boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
          border: "1px solid #e5e7eb"
        }}
      >
        <Space direction="vertical" size="large" style={{ width: "100%" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "8px" }}>
            <div>
              <Title level={3} style={{ margin: 0, background: "linear-gradient(to right, #2563eb, #7c3aed)", WebkitBackgroundClip: "text", WebkitTextFillColor: "transparent" }}>
                {t("users.title")}
              </Title>
              <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px" }}>{t("users.subtitle")}</p>
            </div>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setCreateOpen(true)}
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
              {t("users.addUser")}
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
              style: { marginTop: "16px" }
            }}
            style={{
              borderRadius: "12px",
              overflow: "hidden"
            }}
          />
        </Space>
      </Card>

      {/* 创建用户模态框 */}
      <Modal
        title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("users.createUser")}</span>}
        open={isCreateOpen}
        onCancel={() => {
          setCreateOpen(false);
          createForm.resetFields();
        }}
        onOk={() => createForm.submit()}
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
        <Form
          form={createForm}
          layout="vertical"
          onFinish={(values) => createMutation.mutate(values)}
        >
          <Form.Item
            name="userName"
            label={t("users.username")}
            rules={[{ required: true, message: t("users.usernameRequired") }]}
          >
            <Input placeholder={t("users.usernamePlaceholder")} />
          </Form.Item>
          <Form.Item
            name="email"
            label={t("users.email")}
            rules={[
              { required: true, message: t("users.emailRequired") },
              { type: "email", message: t("validation.email") },
            ]}
          >
            <Input placeholder={t("users.emailPlaceholder")} />
          </Form.Item>
          <Form.Item
            name="password"
            label={t("users.password")}
            rules={[
              { required: true, message: t("users.passwordRequired") },
              { min: 8, message: t("validation.minLength", { field: t("users.password"), min: 8 }) },
            ]}
          >
            <Input.Password placeholder={t("users.passwordPlaceholder")} />
          </Form.Item>
          <Form.Item name="displayName" label={t("users.displayName")}>
            <Input placeholder={t("users.displayNamePlaceholder")} />
          </Form.Item>
          <Form.Item name="emailConfirmed" label={t("users.emailConfirmed")} valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      {/* 编辑用户模态框 */}
      <Modal
        title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("users.editUser")}</span>}
        open={!!editingUser}
        onCancel={() => setEditingUser(null)}
        onOk={() => editForm.submit()}
        confirmLoading={updateMutation.isPending}
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
        {editingUser && (
          <Form
            form={editForm}
            layout="vertical"
            onFinish={(values) =>
              updateMutation.mutate({ userId: editingUser.id, payload: values })
            }
          >
            <Form.Item name="displayName" label={t("users.displayName")}>
              <Input placeholder={t("users.displayNamePlaceholder")} />
            </Form.Item>
            <Form.Item name="emailConfirmed" label={t("users.emailConfirmed")} valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item name="lockoutEnabled" label={t("users.lockoutEnabled")} valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        )}
      </Modal>

      {/* 查看用户详情模态框 */}
      <Modal
        title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("common.details")}</span>}
        open={!!viewingUser}
        onCancel={() => setViewingUser(null)}
        footer={[
          <Button key="close" onClick={() => setViewingUser(null)} style={{ borderRadius: "8px" }}>
            {t("common.close")}
          </Button>,
        ]}
        width={700}
        styles={{
          header: { borderBottom: "1px solid #e5e7eb", paddingBottom: "16px" },
          body: { paddingTop: "24px" }
        }}
      >
        {viewingUser && (
          <Descriptions bordered column={1}>
            <Descriptions.Item label={t("users.userId")}>{viewingUser.id}</Descriptions.Item>
            <Descriptions.Item label={t("users.username")}>{viewingUser.userName}</Descriptions.Item>
            <Descriptions.Item label={t("users.email")}>
              {viewingUser.email}{" "}
              {viewingUser.emailConfirmed && <Badge status="success" text={t("users.emailConfirmed")} />}
            </Descriptions.Item>
            <Descriptions.Item label={t("users.displayName")}>
              {viewingUser.displayName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label={t("users.type")}>
              <Tag color={viewingUser.isExternal ? "blue" : "default"}>
                {viewingUser.isExternal ? t("users.external") : t("users.local")}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label={t("users.status")}>
              {viewingUser.lockoutEnd && new Date(viewingUser.lockoutEnd) > new Date() ? (
                <Tag color="red">{t("users.locked")} {new Date(viewingUser.lockoutEnd).toLocaleString()}</Tag>
              ) : (
                <Tag color="green">{t("users.normal")}</Tag>
              )}
            </Descriptions.Item>
            <Descriptions.Item label={t("users.accessFailed")}>{viewingUser.accessFailedCount}</Descriptions.Item>
            <Descriptions.Item label={t("users.roles")}>
              <Space wrap>
                {viewingUser.roles.length > 0
                  ? viewingUser.roles.map((role) => <Tag key={role}>{role}</Tag>)
                  : "-"}
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label={t("users.linkedAccounts")}>
              {viewingUser.externalLogins.length > 0 ? (
                <Space direction="vertical">
                  {viewingUser.externalLogins.map((login) => (
                    <Tag key={login.providerKey} color="blue">
                      {login.providerDisplayName || login.loginProvider}
                    </Tag>
                  ))}
                </Space>
              ) : (
                "-"
              )}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>

      {/* 修改密码模态框 */}
      <Modal
        title={<span style={{ fontSize: "18px", fontWeight: 600 }}>{t("users.changePasswordTitle")}</span>}
        open={!!passwordUser}
        onCancel={() => {
          setPasswordUser(null);
          passwordForm.resetFields();
        }}
        onOk={() => passwordForm.submit()}
        confirmLoading={passwordMutation.isPending}
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
        {passwordUser && (
          <Form
            form={passwordForm}
            layout="vertical"
            onFinish={(values) =>
              passwordMutation.mutate({ userId: passwordUser.id, payload: values })
            }
          >
            <p>{t("users.changePasswordFor", { username: passwordUser.userName })}</p>
            <Form.Item
              name="newPassword"
              label={t("users.newPassword")}
              rules={[
                { required: true, message: t("users.newPasswordRequired") },
                { min: 8, message: t("validation.minLength", { field: t("users.password"), min: 8 }) },
              ]}
            >
              <Input.Password placeholder={t("users.newPasswordPlaceholder")} />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  );
}
