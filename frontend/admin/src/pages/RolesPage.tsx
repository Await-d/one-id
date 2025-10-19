import { useState } from 'react';
import {
    Table,
    Button,
    Modal,
    Form,
    Input,
    message,
    Space,
    Tag,
    Popconfirm,
    Drawer
} from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { PlusOutlined, EditOutlined, DeleteOutlined, UsergroupAddOutlined, UserOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { rolesApi, type Role, type UserRole, type RoleFormData } from '../lib/rolesApi';

const { TextArea } = Input;

export default function RolesPage() {
    const { t } = useTranslation();
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingRole, setEditingRole] = useState<Role | null>(null);
    const [usersDrawerOpen, setUsersDrawerOpen] = useState(false);
    const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
    const [form] = Form.useForm<RoleFormData>();
    const queryClient = useQueryClient();

    // Fetch roles
    const { data: roles, isLoading } = useQuery<Role[]>({
        queryKey: ['roles'],
        queryFn: () => rolesApi.getAll(),
    });

    // Fetch users in role
    const { data: usersInRole } = useQuery<UserRole[]>({
        queryKey: ['roleUsers', selectedRoleId],
        queryFn: () => selectedRoleId ? rolesApi.getUsersInRole(selectedRoleId) : Promise.resolve([]),
        enabled: !!selectedRoleId,
    });

    // Create/Update mutation
    const saveMutation = useMutation({
        mutationFn: (data: RoleFormData) =>
            editingRole
                ? rolesApi.update(editingRole.id, data)
                : rolesApi.create(data),
        onSuccess: () => {
            message.success(editingRole ? t('roles.updateSuccess') : t('roles.createSuccess'));
            queryClient.invalidateQueries({ queryKey: ['roles'] });
            handleCloseModal();
        },
        onError: (error: Error) => {
            message.error(error.message);
        },
    });

    // Delete mutation
    const deleteMutation = useMutation({
        mutationFn: (id: string) => rolesApi.delete(id),
        onSuccess: () => {
            message.success(t('roles.deleteSuccess'));
            queryClient.invalidateQueries({ queryKey: ['roles'] });
        },
        onError: (error: Error) => {
            message.error(error.message);
        },
    });

    const handleCloseModal = () => {
        setIsModalOpen(false);
        setEditingRole(null);
        form.resetFields();
    };

    const handleEdit = (role: Role) => {
        setEditingRole(role);
        form.setFieldsValue({
            name: role.name,
            description: role.description,
        });
        setIsModalOpen(true);
    };

    const handleCreate = () => {
        form.resetFields();
        setIsModalOpen(true);
    };

    const handleSubmit = async () => {
        try {
            const values = await form.validateFields();
            saveMutation.mutate(values);
        } catch (error) {
            console.error('Validation failed:', error);
        }
    };

    const handleViewUsers = (roleId: string) => {
        setSelectedRoleId(roleId);
        setUsersDrawerOpen(true);
    };

    const columns: ColumnsType<Role> = [
        {
            title: t('roles.name'),
            dataIndex: 'name',
            key: 'name',
            render: (name: string) => <Tag color="blue">{name}</Tag>,
        },
        {
            title: t('roles.description'),
            dataIndex: 'description',
            key: 'description',
            render: (desc?: string) => desc || '-',
        },
        {
            title: t('roles.userCount'),
            dataIndex: 'userCount',
            key: 'userCount',
            render: (count: number) => (
                <Tag icon={<UserOutlined />} color={count > 0 ? 'green' : 'default'}>
                    {t('roles.usersLabel', { count })}
                </Tag>
            ),
        },
        {
            title: t('common.actions'),
            key: 'actions',
            render: (_, record) => (
                <Space>
                    <Button
                        type="link"
                        icon={<UsergroupAddOutlined />}
                        onClick={() => handleViewUsers(record.id)}
                    >
                        {t('roles.users')}
                    </Button>
                    <Button
                        type="link"
                        icon={<EditOutlined />}
                        onClick={() => handleEdit(record)}
                    >
                        {t('common.edit')}
                    </Button>
                    <Popconfirm
                        title={t('roles.deleteRoleTitle')}
                        description={t('roles.deleteRoleDescription', { name: record.name })}
                        onConfirm={() => deleteMutation.mutate(record.id)}
                        okText={t('roles.yes')}
                        cancelText={t('roles.no')}
                    >
                        <Button type="link" danger icon={<DeleteOutlined />}>
                            {t('common.delete')}
                        </Button>
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    const userColumns: ColumnsType<UserRole> = [
        {
            title: t('roles.username'),
            dataIndex: 'userName',
            key: 'userName',
        },
        {
            title: t('roles.email'),
            dataIndex: 'email',
            key: 'email',
        },
        {
            title: t('roles.displayName'),
            dataIndex: 'displayName',
            key: 'displayName',
            render: (name?: string) => name || '-',
        },
    ];

    return (
        <div style={{ padding: '24px' }}>
            <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h1 style={{ margin: 0 }}>{t('roles.title')}</h1>
                <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
                    {t('roles.addRole')}
                </Button>
            </div>

            <Table
                columns={columns}
                dataSource={roles || []}
                rowKey="id"
                loading={isLoading}
            />

            <Modal
                title={editingRole ? t('roles.editRole') : t('roles.createRole')}
                open={isModalOpen}
                onOk={handleSubmit}
                onCancel={handleCloseModal}
                confirmLoading={saveMutation.isPending}
            >
                <Form form={form} layout="vertical">
                    <Form.Item
                        label={t('roles.name')}
                        name="name"
                        rules={[
                            { required: true, message: t('roles.roleNameRequired') },
                            { min: 2, message: t('roles.roleNameMinLength') },
                        ]}
                    >
                        <Input placeholder={t('roles.roleNamePlaceholder')} />
                    </Form.Item>

                    <Form.Item
                        label={t('roles.description')}
                        name="description"
                    >
                        <TextArea rows={3} placeholder={t('roles.descriptionPlaceholder')} />
                    </Form.Item>
                </Form>
            </Modal>

            <Drawer
                title={t('roles.usersInRole', { name: roles?.find(r => r.id === selectedRoleId)?.name })}
                placement="right"
                width={600}
                onClose={() => setUsersDrawerOpen(false)}
                open={usersDrawerOpen}
            >
                <Table
                    columns={userColumns}
                    dataSource={usersInRole || []}
                    rowKey="userId"
                    size="small"
                />
            </Drawer>
        </div>
    );
}

