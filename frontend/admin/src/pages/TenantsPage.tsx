import React, { useState } from "react";
import {
    Table,
    Button,
    Space,
    Modal,
    Form,
    Input,
    Switch,
    message,
    Tag,
    Popconfirm,
} from "antd";
import {
    PlusOutlined,
    EditOutlined,
    DeleteOutlined,
    CheckCircleOutlined,
    StopOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { tenantsApi, type Tenant, type CreateTenantRequest, type UpdateTenantRequest } from "../lib/tenantsApi";
import { useTranslation } from "react-i18next";
import dayjs from "dayjs";

const TenantsPage: React.FC = () => {
    const { t } = useTranslation();
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);
    const [createForm] = Form.useForm();
    const [editForm] = Form.useForm();
    const queryClient = useQueryClient();

    // 获取租户列表
    const { data: tenants = [], isLoading } = useQuery({
        queryKey: ["tenants"],
        queryFn: tenantsApi.getAllTenants,
    });

    // 创建租户
    const createMutation = useMutation({
        mutationFn: tenantsApi.createTenant,
        onSuccess: () => {
            message.success(t("tenants.createSuccess"));
            queryClient.invalidateQueries({ queryKey: ["tenants"] });
            setIsCreateModalOpen(false);
            createForm.resetFields();
        },
        onError: () => {
            message.error(t("tenants.createFailed"));
        },
    });

    // 更新租户
    const updateMutation = useMutation({
        mutationFn: ({ id, data }: { id: string; data: UpdateTenantRequest }) =>
            tenantsApi.updateTenant(id, data),
        onSuccess: () => {
            message.success(t("tenants.updateSuccess"));
            queryClient.invalidateQueries({ queryKey: ["tenants"] });
            setIsEditModalOpen(false);
            setEditingTenant(null);
            editForm.resetFields();
        },
        onError: () => {
            message.error(t("tenants.updateFailed"));
        },
    });

    // 删除租户
    const deleteMutation = useMutation({
        mutationFn: tenantsApi.deleteTenant,
        onSuccess: () => {
            message.success(t("tenants.deleteSuccess"));
            queryClient.invalidateQueries({ queryKey: ["tenants"] });
        },
        onError: () => {
            message.error(t("tenants.deleteFailed"));
        },
    });

    // 切换租户状态
    const toggleStatusMutation = useMutation({
        mutationFn: tenantsApi.toggleTenantStatus,
        onSuccess: () => {
            message.success(t("tenants.statusToggleSuccess"));
            queryClient.invalidateQueries({ queryKey: ["tenants"] });
        },
        onError: () => {
            message.error(t("tenants.statusToggleFailed"));
        },
    });

    const handleCreate = async (values: CreateTenantRequest) => {
        createMutation.mutate(values);
    };

    const handleEdit = async (values: UpdateTenantRequest) => {
        if (editingTenant) {
            updateMutation.mutate({ id: editingTenant.id, data: values });
        }
    };

    const handleDelete = (id: string) => {
        deleteMutation.mutate(id);
    };

    const handleToggleStatus = (id: string) => {
        toggleStatusMutation.mutate(id);
    };

    const showEditModal = (tenant: Tenant) => {
        setEditingTenant(tenant);
        editForm.setFieldsValue({
            displayName: tenant.displayName,
            domain: tenant.domain,
            isActive: tenant.isActive,
        });
        setIsEditModalOpen(true);
    };

    const columns = [
        {
            title: t("tenants.name"),
            dataIndex: "name",
            key: "name",
        },
        {
            title: t("tenants.displayName"),
            dataIndex: "displayName",
            key: "displayName",
        },
        {
            title: t("tenants.domain"),
            dataIndex: "domain",
            key: "domain",
        },
        {
            title: t("tenants.status"),
            dataIndex: "isActive",
            key: "isActive",
            render: (isActive: boolean) =>
                isActive ? (
                    <Tag icon={<CheckCircleOutlined />} color="success">
                        {t("tenants.active")}
                    </Tag>
                ) : (
                    <Tag icon={<StopOutlined />} color="default">
                        {t("tenants.inactive")}
                    </Tag>
                ),
        },
        {
            title: t("tenants.createdAt"),
            dataIndex: "createdAt",
            key: "createdAt",
            render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
        },
        {
            title: t("tenants.actions"),
            key: "actions",
            render: (_: unknown, record: Tenant) => (
                <Space>
                    <Button
                        type="link"
                        icon={<EditOutlined />}
                        onClick={() => showEditModal(record)}
                    >
                        {t("common.edit")}
                    </Button>
                    <Popconfirm
                        title={t("tenants.toggleConfirm")}
                        onConfirm={() => handleToggleStatus(record.id)}
                    >
                        <Button type="link">
                            {record.isActive
                                ? t("common.disable")
                                : t("common.enable")}
                        </Button>
                    </Popconfirm>
                    <Popconfirm
                        title={t("tenants.deleteConfirm")}
                        onConfirm={() => handleDelete(record.id)}
                    >
                        <Button type="link" danger icon={<DeleteOutlined />}>
                            {t("common.delete")}
                        </Button>
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    return (
        <div>
            <div style={{ marginBottom: 16, display: "flex", justifyContent: "space-between" }}>
                <h2>{t("tenants.title")}</h2>
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setIsCreateModalOpen(true)}
                >
                    {t("tenants.create")}
                </Button>
            </div>

            <Table
                columns={columns}
                dataSource={tenants}
                rowKey="id"
                loading={isLoading}
            />

            {/* 创建租户模态框 */}
            <Modal
                title={t("tenants.create")}
                open={isCreateModalOpen}
                onOk={() => createForm.submit()}
                onCancel={() => {
                    setIsCreateModalOpen(false);
                    createForm.resetFields();
                }}
                confirmLoading={createMutation.isPending}
            >
                <Form form={createForm} layout="vertical" onFinish={handleCreate}>
                    <Form.Item
                        name="name"
                        label={t("tenants.name")}
                        rules={[
                            { required: true, message: t("tenants.nameRequired") },
                            {
                                pattern: /^[a-zA-Z0-9_-]+$/,
                                message: t("tenants.nameFormat"),
                            },
                        ]}
                    >
                        <Input placeholder={t("tenants.namePlaceholder")} />
                    </Form.Item>
                    <Form.Item
                        name="displayName"
                        label={t("tenants.displayName")}
                        rules={[
                            { required: true, message: t("tenants.displayNameRequired") },
                        ]}
                    >
                        <Input placeholder={t("tenants.displayNamePlaceholder")} />
                    </Form.Item>
                    <Form.Item
                        name="domain"
                        label={t("tenants.domain")}
                        rules={[
                            { required: true, message: t("tenants.domainRequired") },
                        ]}
                    >
                        <Input placeholder={t("tenants.domainPlaceholder")} />
                    </Form.Item>
                </Form>
            </Modal>

            {/* 编辑租户模态框 */}
            <Modal
                title={t("tenants.edit")}
                open={isEditModalOpen}
                onOk={() => editForm.submit()}
                onCancel={() => {
                    setIsEditModalOpen(false);
                    setEditingTenant(null);
                    editForm.resetFields();
                }}
                confirmLoading={updateMutation.isPending}
            >
                <Form form={editForm} layout="vertical" onFinish={handleEdit}>
                    <Form.Item
                        name="displayName"
                        label={t("tenants.displayName")}
                        rules={[
                            { required: true, message: t("tenants.displayNameRequired") },
                        ]}
                    >
                        <Input />
                    </Form.Item>
                    <Form.Item
                        name="domain"
                        label={t("tenants.domain")}
                        rules={[
                            { required: true, message: t("tenants.domainRequired") },
                        ]}
                    >
                        <Input />
                    </Form.Item>
                    <Form.Item
                        name="isActive"
                        label={t("tenants.status")}
                        valuePropName="checked"
                    >
                        <Switch
                            checkedChildren={t("tenants.active")}
                            unCheckedChildren={t("tenants.inactive")}
                        />
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    );
};

export default TenantsPage;

