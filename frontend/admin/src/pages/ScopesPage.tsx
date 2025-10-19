import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Table, Button, Modal, Form, Input, message, Space, Popconfirm, Tag } from "antd";
import { PlusOutlined, EditOutlined, DeleteOutlined } from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { apiClient } from "../lib/apiClient";

interface Scope {
  name: string;
  displayName: string;
  description?: string;
  resources: string[];
}

interface ScopeFormValues {
  name: string;
  displayName: string;
  description?: string;
  resources?: string;
}

const ScopesPage: React.FC = () => {
  const { t } = useTranslation();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingScope, setEditingScope] = useState<Scope | null>(null);
  const [form] = Form.useForm<ScopeFormValues>();
  const queryClient = useQueryClient();

  const { data: scopes, isLoading } = useQuery<Scope[]>({
    queryKey: ["scopes"],
    queryFn: () => apiClient.get<Scope[]>("/api/scopes"),
  });

  const createMutation = useMutation({
    mutationFn: (values: ScopeFormValues) =>
      apiClient.post("/api/scopes", {
        name: values.name,
        displayName: values.displayName,
        description: values.description,
        resources: values.resources
          ? values.resources.split(",").map((r) => r.trim())
          : [],
      }),
    onSuccess: () => {
      message.success(t("scopes.createSuccess"));
      queryClient.invalidateQueries({ queryKey: ["scopes"] });
      setIsModalVisible(false);
      form.resetFields();
    },
    onError: (error: Error) => {
      message.error(error.message);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ name, values }: { name: string; values: ScopeFormValues }) =>
      apiClient.put(`/api/scopes/${name}`, {
        displayName: values.displayName,
        description: values.description,
        resources: values.resources
          ? values.resources.split(",").map((r) => r.trim())
          : [],
      }),
    onSuccess: () => {
      message.success(t("scopes.updateSuccess"));
      queryClient.invalidateQueries({ queryKey: ["scopes"] });
      setIsModalVisible(false);
      setEditingScope(null);
      form.resetFields();
    },
    onError: (error: Error) => {
      message.error(error.message);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (name: string) => apiClient.delete(`/api/scopes/${name}`),
    onSuccess: () => {
      message.success(t("scopes.deleteSuccess"));
      queryClient.invalidateQueries({ queryKey: ["scopes"] });
    },
    onError: (error: Error) => {
      message.error(error.message);
    },
  });

  const handleCreate = () => {
    setEditingScope(null);
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleEdit = (scope: Scope) => {
    setEditingScope(scope);
    form.setFieldsValue({
      name: scope.name,
      displayName: scope.displayName,
      description: scope.description,
      resources: scope.resources.join(", "),
    });
    setIsModalVisible(true);
  };

  const handleDelete = (name: string) => {
    deleteMutation.mutate(name);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingScope) {
        updateMutation.mutate({ name: editingScope.name, values });
      } else {
        createMutation.mutate(values);
      }
    } catch (error) {
      console.error("Validation failed:", error);
    }
  };

  const columns = [
    {
      title: t("scopes.name"),
      dataIndex: "name",
      key: "name",
      render: (name: string) => {
        const builtInScopes = ["openid", "profile", "email", "offline_access", "admin_api"];
        return (
          <Space>
            <span>{name}</span>
            {builtInScopes.includes(name) && (
              <Tag color="blue">{t("scopes.builtIn")}</Tag>
            )}
          </Space>
        );
      },
    },
    {
      title: t("scopes.displayName"),
      dataIndex: "displayName",
      key: "displayName",
    },
    {
      title: t("scopes.description"),
      dataIndex: "description",
      key: "description",
      ellipsis: true,
    },
    {
      title: t("scopes.resources"),
      dataIndex: "resources",
      key: "resources",
      render: (resources: string[]) => (
        <Space size={[0, 8]} wrap>
          {resources.map((resource) => (
            <Tag key={resource}>{resource}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: t("common.actions"),
      key: "actions",
      render: (_: unknown, record: Scope) => {
        const builtInScopes = ["openid", "profile", "email", "offline_access", "admin_api"];
        const isBuiltIn = builtInScopes.includes(record.name);

        return (
          <Space>
            <Button
              type="link"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            >
              {t("common.edit")}
            </Button>
            <Popconfirm
              title={t("scopes.confirmDelete")}
              onConfirm={() => handleDelete(record.name)}
              disabled={isBuiltIn}
            >
              <Button
                type="link"
                danger
                icon={<DeleteOutlined />}
                disabled={isBuiltIn}
              >
                {t("common.delete")}
              </Button>
            </Popconfirm>
          </Space>
        );
      },
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          {t("scopes.create")}
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={scopes}
        rowKey="name"
        loading={isLoading}
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={
          editingScope
            ? t("scopes.editTitle")
            : t("scopes.createTitle")
        }
        open={isModalVisible}
        onOk={handleSubmit}
        onCancel={() => {
          setIsModalVisible(false);
          setEditingScope(null);
          form.resetFields();
        }}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label={t("scopes.name")}
            name="name"
            rules={[
              { required: true, message: t("scopes.nameRequired") },
              {
                pattern: /^[a-z0-9_]+$/,
                message: t("scopes.namePattern"),
              },
            ]}
          >
            <Input disabled={!!editingScope} placeholder="e.g., custom_api" />
          </Form.Item>

          <Form.Item
            label={t("scopes.displayName")}
            name="displayName"
            rules={[
              {
                required: true,
                message: t("scopes.displayNameRequired"),
              },
            ]}
          >
            <Input placeholder="e.g., Custom API Access" />
          </Form.Item>

          <Form.Item
            label={t("scopes.description")}
            name="description"
          >
            <Input.TextArea
              rows={3}
              placeholder={t("scopes.descriptionPlaceholder")}
            />
          </Form.Item>

          <Form.Item
            label={t("scopes.resources")}
            name="resources"
            tooltip={t("scopes.resourcesTooltip")}
          >
            <Input placeholder="e.g., api1, api2" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default ScopesPage;

