import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Card,
  Tabs,
  Table,
  Button,
  Form,
  Input,
  Select,
  Switch,
  Modal,
  message,
  Space,
  Tag,
  Popconfirm,
  Typography,
  InputNumber,
  TimePicker,
  Checkbox,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
  SafetyOutlined,
  ClockCircleOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  StopOutlined,
} from "@ant-design/icons";
import { loginPoliciesApi, type IpAccessRule, type LoginTimeRestriction } from "../lib/loginPoliciesApi";
import dayjs from "dayjs";

const { Title, Text } = Typography;
const { TextArea } = Input;

export default function LoginPoliciesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("ip-rules");
  const [ipRuleModalOpen, setIpRuleModalOpen] = useState(false);
  const [timeRestrictionModalOpen, setTimeRestrictionModalOpen] = useState(false);
  const [editingIpRule, setEditingIpRule] = useState<IpAccessRule | null>(null);
  const [editingTimeRestriction, setEditingTimeRestriction] = useState<LoginTimeRestriction | null>(null);

  const [ipRuleForm] = Form.useForm();
  const [timeRestrictionForm] = Form.useForm();

  // IP Rules Queries
  const { data: ipRules, isLoading: ipRulesLoading } = useQuery({
    queryKey: ["ipAccessRules"],
    queryFn: () => loginPoliciesApi.ipRules.getAll(),
  });

  const createIpRuleMutation = useMutation({
    mutationFn: loginPoliciesApi.ipRules.create,
    onSuccess: () => {
      message.success(t("loginPolicies.createSuccess"));
      queryClient.invalidateQueries({ queryKey: ["ipAccessRules"] });
      setIpRuleModalOpen(false);
      ipRuleForm.resetFields();
    },
  });

  const updateIpRuleMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => loginPoliciesApi.ipRules.update(id, data),
    onSuccess: () => {
      message.success(t("loginPolicies.updateSuccess"));
      queryClient.invalidateQueries({ queryKey: ["ipAccessRules"] });
      setIpRuleModalOpen(false);
      setEditingIpRule(null);
      ipRuleForm.resetFields();
    },
  });

  const deleteIpRuleMutation = useMutation({
    mutationFn: loginPoliciesApi.ipRules.delete,
    onSuccess: () => {
      message.success(t("loginPolicies.deleteSuccess"));
      queryClient.invalidateQueries({ queryKey: ["ipAccessRules"] });
    },
  });

  const toggleIpRuleMutation = useMutation({
    mutationFn: loginPoliciesApi.ipRules.toggleEnabled,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ipAccessRules"] });
    },
  });

  // Time Restrictions Queries
  const { data: timeRestrictions, isLoading: timeRestrictionsLoading } = useQuery({
    queryKey: ["timeRestrictions"],
    queryFn: () => loginPoliciesApi.timeRestrictions.getAll(),
  });

  const createTimeRestrictionMutation = useMutation({
    mutationFn: loginPoliciesApi.timeRestrictions.create,
    onSuccess: () => {
      message.success(t("loginPolicies.createSuccess"));
      queryClient.invalidateQueries({ queryKey: ["timeRestrictions"] });
      setTimeRestrictionModalOpen(false);
      timeRestrictionForm.resetFields();
    },
  });

  const updateTimeRestrictionMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      loginPoliciesApi.timeRestrictions.update(id, data),
    onSuccess: () => {
      message.success(t("loginPolicies.updateSuccess"));
      queryClient.invalidateQueries({ queryKey: ["timeRestrictions"] });
      setTimeRestrictionModalOpen(false);
      setEditingTimeRestriction(null);
      timeRestrictionForm.resetFields();
    },
  });

  const deleteTimeRestrictionMutation = useMutation({
    mutationFn: loginPoliciesApi.timeRestrictions.delete,
    onSuccess: () => {
      message.success(t("loginPolicies.deleteSuccess"));
      queryClient.invalidateQueries({ queryKey: ["timeRestrictions"] });
    },
  });

  const toggleTimeRestrictionMutation = useMutation({
    mutationFn: loginPoliciesApi.timeRestrictions.toggleEnabled,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["timeRestrictions"] });
    },
  });

  // IP Rules Handlers
  const handleCreateIpRule = () => {
    setEditingIpRule(null);
    ipRuleForm.resetFields();
    setIpRuleModalOpen(true);
  };

  const handleEditIpRule = (rule: IpAccessRule) => {
    setEditingIpRule(rule);
    ipRuleForm.setFieldsValue(rule);
    setIpRuleModalOpen(true);
  };

  const handleSaveIpRule = () => {
    ipRuleForm.validateFields().then((values) => {
      if (editingIpRule) {
        updateIpRuleMutation.mutate({ id: editingIpRule.id, data: values });
      } else {
        createIpRuleMutation.mutate(values);
      }
    });
  };

  const handleDeleteIpRule = (id: number) => {
    deleteIpRuleMutation.mutate(id);
  };

  const handleToggleIpRule = (id: number) => {
    toggleIpRuleMutation.mutate(id);
  };

  // Time Restriction Handlers
  const handleCreateTimeRestriction = () => {
    setEditingTimeRestriction(null);
    timeRestrictionForm.resetFields();
    setTimeRestrictionModalOpen(true);
  };

  const handleEditTimeRestriction = (restriction: LoginTimeRestriction) => {
    setEditingTimeRestriction(restriction);
    const daysArray = restriction.allowedDaysOfWeek?.split(",").map((d) => parseInt(d.trim())) || [];
    timeRestrictionForm.setFieldsValue({
      ...restriction,
      allowedDaysOfWeek: daysArray,
      dailyStartTime: restriction.dailyStartTime ? dayjs(restriction.dailyStartTime, "HH:mm") : null,
      dailyEndTime: restriction.dailyEndTime ? dayjs(restriction.dailyEndTime, "HH:mm") : null,
    });
    setTimeRestrictionModalOpen(true);
  };

  const handleSaveTimeRestriction = () => {
    timeRestrictionForm.validateFields().then((values) => {
      const payload = {
        ...values,
        allowedDaysOfWeek: values.allowedDaysOfWeek?.join(","),
        dailyStartTime: values.dailyStartTime?.format("HH:mm"),
        dailyEndTime: values.dailyEndTime?.format("HH:mm"),
      };

      if (editingTimeRestriction) {
        updateTimeRestrictionMutation.mutate({ id: editingTimeRestriction.id, data: payload });
      } else {
        createTimeRestrictionMutation.mutate(payload);
      }
    });
  };

  const handleDeleteTimeRestriction = (id: number) => {
    deleteTimeRestrictionMutation.mutate(id);
  };

  const handleToggleTimeRestriction = (id: number) => {
    toggleTimeRestrictionMutation.mutate(id);
  };

  // IP Rules Columns
  const ipRulesColumns: ColumnsType<IpAccessRule> = [
    {
      title: t("loginPolicies.ipAddress"),
      dataIndex: "ipAddress",
      key: "ipAddress",
    },
    {
      title: t("loginPolicies.ruleType"),
      dataIndex: "ruleType",
      key: "ruleType",
      render: (type: string) => (
        <Tag color={type === "Whitelist" ? "green" : "red"}>
          {t(`loginPolicies.ruleTypes.${type}`) || type}
        </Tag>
      ),
    },
    {
      title: t("loginPolicies.description"),
      dataIndex: "description",
      key: "description",
      ellipsis: true,
    },
    {
      title: t("loginPolicies.enabled"),
      dataIndex: "isEnabled",
      key: "isEnabled",
      render: (isEnabled: boolean, record) => (
        <Switch
          checked={isEnabled}
          onChange={() => handleToggleIpRule(record.id)}
          checkedChildren={<CheckCircleOutlined />}
          unCheckedChildren={<StopOutlined />}
        />
      ),
    },
    {
      title: t("loginPolicies.createdAt"),
      dataIndex: "createdAt",
      key: "createdAt",
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm"),
    },
    {
      title: t("common.actions"),
      key: "actions",
      render: (_, record) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            size="small"
            onClick={() => handleEditIpRule(record)}
          >
            {t("common.edit")}
          </Button>
          <Popconfirm
            title={t("loginPolicies.confirmDelete")}
            onConfirm={() => handleDeleteIpRule(record.id)}
          >
            <Button icon={<DeleteOutlined />} size="small" danger>
              {t("common.delete")}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  // Time Restrictions Columns
  const timeRestrictionsColumns: ColumnsType<LoginTimeRestriction> = [
    {
      title: t("loginPolicies.ruleName"),
      dataIndex: "ruleName",
      key: "ruleName",
    },
    {
      title: t("loginPolicies.allowedDays"),
      dataIndex: "allowedDaysOfWeek",
      key: "allowedDaysOfWeek",
      render: (days: string) => {
        if (!days) return "-";
        const dayNames = [
          t("loginPolicies.days.monday"),
          t("loginPolicies.days.tuesday"),
          t("loginPolicies.days.wednesday"),
          t("loginPolicies.days.thursday"),
          t("loginPolicies.days.friday"),
          t("loginPolicies.days.saturday"),
          t("loginPolicies.days.sunday"),
        ];
        const dayList = days.split(",").map((d) => dayNames[parseInt(d)]);
        return dayList.join(", ");
      },
    },
    {
      title: t("loginPolicies.timeRange"),
      key: "timeRange",
      render: (_, record) => {
        if (record.dailyStartTime && record.dailyEndTime) {
          return `${record.dailyStartTime} - ${record.dailyEndTime}`;
        }
        return "-";
      },
    },
    {
      title: t("loginPolicies.priority"),
      dataIndex: "priority",
      key: "priority",
    },
    {
      title: t("loginPolicies.enabled"),
      dataIndex: "isEnabled",
      key: "isEnabled",
      render: (isEnabled: boolean, record) => (
        <Switch
          checked={isEnabled}
          onChange={() => handleToggleTimeRestriction(record.id)}
          checkedChildren={<CheckCircleOutlined />}
          unCheckedChildren={<StopOutlined />}
        />
      ),
    },
    {
      title: t("loginPolicies.createdAt"),
      dataIndex: "createdAt",
      key: "createdAt",
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm"),
    },
    {
      title: t("common.actions"),
      key: "actions",
      render: (_, record) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            size="small"
            onClick={() => handleEditTimeRestriction(record)}
          >
            {t("common.edit")}
          </Button>
          <Popconfirm
            title={t("loginPolicies.confirmDelete")}
            onConfirm={() => handleDeleteTimeRestriction(record.id)}
          >
            <Button icon={<DeleteOutlined />} size="small" danger>
              {t("common.delete")}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const tabItems = [
    {
      key: "ip-rules",
      label: (
        <span>
          <SafetyOutlined />
          {t("loginPolicies.ipRules")}
        </span>
      ),
      children: (
        <div>
          <div style={{ marginBottom: 16 }}>
            <Button icon={<PlusOutlined />} type="primary" onClick={handleCreateIpRule}>
              {t("loginPolicies.createIpRule")}
            </Button>
          </div>
          <Table
            rowKey="id"
            columns={ipRulesColumns}
            dataSource={ipRules}
            loading={ipRulesLoading}
            pagination={{ pageSize: 10 }}
          />
        </div>
      ),
    },
    {
      key: "time-restrictions",
      label: (
        <span>
          <ClockCircleOutlined />
          {t("loginPolicies.timeRestrictions")}
        </span>
      ),
      children: (
        <div>
          <div style={{ marginBottom: 16 }}>
            <Button icon={<PlusOutlined />} type="primary" onClick={handleCreateTimeRestriction}>
              {t("loginPolicies.createTimeRestriction")}
            </Button>
          </div>
          <Table
            rowKey="id"
            columns={timeRestrictionsColumns}
            dataSource={timeRestrictions}
            loading={timeRestrictionsLoading}
            pagination={{ pageSize: 10 }}
          />
        </div>
      ),
    },
  ];

  return (
    <div style={{ padding: "24px" }}>
      <Card>
        <div style={{ marginBottom: 24 }}>
          <Title level={3} style={{ margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
            <SafetyOutlined />
            {t("loginPolicies.title")}
          </Title>
          <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px", fontWeight: 400 }}>
            {t("loginPolicies.subtitle")}
          </p>
        </div>

        <Tabs activeKey={activeTab} onChange={setActiveTab} items={tabItems} />

        {/* IP Rule Modal */}
        <Modal
          title={editingIpRule ? t("loginPolicies.editIpRule") : t("loginPolicies.createIpRule")}
          open={ipRuleModalOpen}
          onCancel={() => {
            setIpRuleModalOpen(false);
            ipRuleForm.resetFields();
            setEditingIpRule(null);
          }}
          onOk={handleSaveIpRule}
          okText={t("common.save")}
          cancelText={t("common.cancel")}
        >
          <Form form={ipRuleForm} layout="vertical">
            <Form.Item
              label={t("loginPolicies.ipAddress")}
              name="ipAddress"
              rules={[{ required: true, message: t("loginPolicies.ipAddressRequired") }]}
            >
              <Input placeholder={t("loginPolicies.ipAddressPlaceholder")} />
            </Form.Item>

            <Form.Item
              label={t("loginPolicies.ruleType")}
              name="ruleType"
              rules={[{ required: true, message: t("loginPolicies.ruleTypeRequired") }]}
            >
              <Select placeholder={t("loginPolicies.ruleTypeRequired")}>
                <Select.Option value="Whitelist">{t("loginPolicies.ruleTypes.Whitelist")}</Select.Option>
                <Select.Option value="Blacklist">{t("loginPolicies.ruleTypes.Blacklist")}</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item label={t("loginPolicies.description")} name="description">
              <TextArea rows={3} placeholder={t("loginPolicies.descriptionPlaceholder")} />
            </Form.Item>

            <Form.Item label={t("loginPolicies.enabled")} name="isEnabled" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>

        {/* Time Restriction Modal */}
        <Modal
          title={
            editingTimeRestriction
              ? t("loginPolicies.editTimeRestriction")
              : t("loginPolicies.createTimeRestriction")
          }
          open={timeRestrictionModalOpen}
          onCancel={() => {
            setTimeRestrictionModalOpen(false);
            timeRestrictionForm.resetFields();
            setEditingTimeRestriction(null);
          }}
          onOk={handleSaveTimeRestriction}
          okText={t("common.save")}
          cancelText={t("common.cancel")}
        >
          <Form form={timeRestrictionForm} layout="vertical">
            <Form.Item
              label={t("loginPolicies.ruleName")}
              name="ruleName"
              rules={[{ required: true, message: t("loginPolicies.ruleNameRequired") }]}
            >
              <Input placeholder={t("loginPolicies.ruleNamePlaceholder")} />
            </Form.Item>

            <Form.Item
              label={t("loginPolicies.allowedDays")}
              name="allowedDaysOfWeek"
              rules={[{ required: true, message: t("loginPolicies.allowedDaysRequired") }]}
            >
              <Checkbox.Group style={{ width: "100%" }}>
                <Space direction="vertical">
                  <Checkbox value={1}>{t("loginPolicies.days.monday")}</Checkbox>
                  <Checkbox value={2}>{t("loginPolicies.days.tuesday")}</Checkbox>
                  <Checkbox value={3}>{t("loginPolicies.days.wednesday")}</Checkbox>
                  <Checkbox value={4}>{t("loginPolicies.days.thursday")}</Checkbox>
                  <Checkbox value={5}>{t("loginPolicies.days.friday")}</Checkbox>
                  <Checkbox value={6}>{t("loginPolicies.days.saturday")}</Checkbox>
                  <Checkbox value={0}>{t("loginPolicies.days.sunday")}</Checkbox>
                </Space>
              </Checkbox.Group>
            </Form.Item>

            <Form.Item
              label={t("loginPolicies.timeRange")}
              required
              style={{ marginBottom: 0 }}
            >
              <Space>
                <Form.Item
                  name="dailyStartTime"
                  rules={[{ required: true, message: t("loginPolicies.timeRangeRequired") }]}
                >
                  <TimePicker format="HH:mm" />
                </Form.Item>
                <span>-</span>
                <Form.Item
                  name="dailyEndTime"
                  rules={[{ required: true, message: t("loginPolicies.timeRangeRequired") }]}
                >
                  <TimePicker format="HH:mm" />
                </Form.Item>
              </Space>
            </Form.Item>

            <Form.Item label={t("loginPolicies.description")} name="description">
              <TextArea rows={3} placeholder={t("loginPolicies.descriptionPlaceholder")} />
            </Form.Item>

            <Form.Item
              label={t("loginPolicies.priority")}
              name="priority"
              help={t("loginPolicies.priorityHelp")}
            >
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>

            <Form.Item label={t("loginPolicies.enabled")} name="isEnabled" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      </Card>
    </div>
  );
}
