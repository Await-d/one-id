import { useState } from "react";
import { Button, Modal, Select, DatePicker, Switch, Space, message, Table, Tag, Alert } from "antd";
import { useTranslation } from "react-i18next";
import type { UserSummary } from "../types/users";
import {
  UsergroupAddOutlined,
  UsergroupDeleteOutlined,
  CheckCircleOutlined,
  StopOutlined,
  LockOutlined,
  UnlockOutlined,
  LogoutOutlined,
  KeyOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
} from "@ant-design/icons";
import { bulkOperationsApi, type BulkOperationResult } from "../lib/bulkOperationsApi";
import { rolesApi } from "../lib/rolesApi";
import { useQuery } from "@tanstack/react-query";
import dayjs, { Dayjs } from "dayjs";

interface BulkUserOperationsProps {
  selectedUsers: UserSummary[];
  onComplete: () => void;
}

export function BulkUserOperations({ selectedUsers, onComplete }: BulkUserOperationsProps) {
  const { t } = useTranslation();
  const [operation, setOperation] = useState<string | null>(null);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [lockoutEnd, setLockoutEnd] = useState<Dayjs | null>(null);
  const [sendEmail, setSendEmail] = useState(true);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<BulkOperationResult | null>(null);

  const { data: roles } = useQuery({
    queryKey: ["roles"],
    queryFn: () => rolesApi.getAll(),
  });

  const userIds = selectedUsers.map((u) => u.id);

  const handleOperation = async () => {
    if (userIds.length === 0) {
      message.warning(t('bulkOperations.selectAtLeastOneUser'));
      return;
    }

    setLoading(true);
    setResult(null);

    try {
      let operationResult: BulkOperationResult;

      switch (operation) {
        case "assign-roles":
          if (selectedRoles.length === 0) {
            message.warning(t('bulkOperations.selectAtLeastOneRole'));
            setLoading(false);
            return;
          }
          operationResult = await bulkOperationsApi.assignRoles({
            userIds,
            roleNames: selectedRoles,
          });
          break;

        case "remove-roles":
          if (selectedRoles.length === 0) {
            message.warning(t('bulkOperations.selectAtLeastOneRole'));
            setLoading(false);
            return;
          }
          operationResult = await bulkOperationsApi.removeRoles({
            userIds,
            roleNames: selectedRoles,
          });
          break;

        case "enable":
          operationResult = await bulkOperationsApi.enableUsers(userIds);
          break;

        case "disable":
          operationResult = await bulkOperationsApi.disableUsers(userIds);
          break;

        case "lock":
          operationResult = await bulkOperationsApi.lockUsers({
            userIds,
            lockoutEndUtc: lockoutEnd ? lockoutEnd.toISOString() : undefined,
          });
          break;

        case "unlock":
          operationResult = await bulkOperationsApi.unlockUsers(userIds);
          break;

        case "revoke-sessions":
          operationResult = await bulkOperationsApi.revokeSessions(userIds);
          break;

        case "reset-passwords":
          operationResult = await bulkOperationsApi.resetPasswords({
            userIds,
            sendEmail,
          });
          break;

        case "delete":
          operationResult = await bulkOperationsApi.deleteUsers(userIds);
          break;

        default:
          message.error(t('bulkOperations.invalidOperation'));
          setLoading(false);
          return;
      }

      setResult(operationResult);

      if (operationResult.success) {
        message.success(operationResult.message);
        setTimeout(() => {
          setOperation(null);
          onComplete();
        }, 2000);
      } else {
        message.warning(operationResult.message);
      }
    } catch (error: any) {
      message.error(t('bulkOperations.operationFailed', { message: error.message }));
    } finally {
      setLoading(false);
    }
  };

  const renderOperationContent = () => {
    switch (operation) {
      case "assign-roles":
      case "remove-roles":
        return (
          <div>
            <p>{operation === "assign-roles"
              ? t('bulkOperations.selectRolesToAssign', { count: userIds.length })
              : t('bulkOperations.selectRolesToRemove', { count: userIds.length })
            }</p>
            <Select
              mode="multiple"
              style={{ width: "100%" }}
              placeholder={t('bulkOperations.selectRoles')}
              value={selectedRoles}
              onChange={setSelectedRoles}
              options={roles?.map((role: any) => ({ label: role.name, value: role.name }))}
            />
          </div>
        );

      case "lock":
        return (
          <div>
            <p>{t('bulkOperations.lockUserAccounts', { count: userIds.length })}</p>
            <Space direction="vertical" style={{ width: "100%" }}>
              <div>
                <label>{t('bulkOperations.lockoutUntil')}</label>
                <DatePicker
                  showTime
                  style={{ width: "100%", marginTop: 8 }}
                  value={lockoutEnd}
                  onChange={setLockoutEnd}
                  disabledDate={(current) => current && current < dayjs().startOf("day")}
                />
              </div>
            </Space>
          </div>
        );

      case "reset-passwords":
        return (
          <div>
            <p>{t('bulkOperations.resetPasswordsFor', { count: userIds.length })}</p>
            <div style={{ marginTop: 16 }}>
              <Switch checked={sendEmail} onChange={setSendEmail} />
              <span style={{ marginLeft: 8 }}>{t('bulkOperations.sendResetEmail')}</span>
            </div>
          </div>
        );

      case "delete":
        return (
          <div>
            <Alert
              message={t('bulkOperations.warning')}
              description={t('bulkOperations.deleteWarning', { count: userIds.length })}
              type="warning"
              showIcon
              icon={<ExclamationCircleOutlined />}
              style={{ marginBottom: 16 }}
            />
            <p>{t('bulkOperations.selectedUsers')}</p>
            <ul>
              {selectedUsers.slice(0, 5).map((user) => (
                <li key={user.id}>
                  {user.email} ({user.userName})
                </li>
              ))}
              {selectedUsers.length > 5 && <li>{t('bulkOperations.andMore', { count: selectedUsers.length - 5 })}</li>}
            </ul>
          </div>
        );

      default:
        return (
          <div>
            <p>{t('bulkOperations.confirmOperation', { count: userIds.length, operation })}</p>
            <ul>
              {selectedUsers.slice(0, 5).map((user) => (
                <li key={user.id}>
                  {user.email} ({user.userName})
                </li>
              ))}
              {selectedUsers.length > 5 && <li>{t('bulkOperations.andMore', { count: selectedUsers.length - 5 })}</li>}
            </ul>
          </div>
        );
    }
  };

  const getOperationTitle = () => {
    const titles: Record<string, string> = {
      "assign-roles": t('bulkOperations.assignRoles'),
      "remove-roles": t('bulkOperations.removeRoles'),
      "enable": t('bulkOperations.enableUsers'),
      "disable": t('bulkOperations.disableUsers'),
      "lock": t('bulkOperations.lockUsers'),
      "unlock": t('bulkOperations.unlockUsers'),
      "revoke-sessions": t('bulkOperations.revokeSessions'),
      "reset-passwords": t('bulkOperations.resetPasswords'),
      "delete": t('bulkOperations.deleteUsers'),
    };
    return titles[operation || ""] || t('bulkOperations.bulkOperation');
  };

  return (
    <div>
      <Space wrap>
        <Button
          icon={<UsergroupAddOutlined />}
          onClick={() => setOperation("assign-roles")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.assignRoles')}
        </Button>
        <Button
          icon={<UsergroupDeleteOutlined />}
          onClick={() => setOperation("remove-roles")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.removeRoles')}
        </Button>
        <Button
          icon={<CheckCircleOutlined />}
          onClick={() => setOperation("enable")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.enable')}
        </Button>
        <Button
          icon={<StopOutlined />}
          onClick={() => setOperation("disable")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.disable')}
        </Button>
        <Button
          icon={<LockOutlined />}
          onClick={() => setOperation("lock")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.lock')}
        </Button>
        <Button
          icon={<UnlockOutlined />}
          onClick={() => setOperation("unlock")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.unlock')}
        </Button>
        <Button
          icon={<LogoutOutlined />}
          onClick={() => setOperation("revoke-sessions")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.revokeSessions')}
        </Button>
        <Button
          icon={<KeyOutlined />}
          onClick={() => setOperation("reset-passwords")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.resetPasswords')}
        </Button>
        <Button
          icon={<DeleteOutlined />}
          danger
          onClick={() => setOperation("delete")}
          disabled={userIds.length === 0}
        >
          {t('bulkOperations.delete')}
        </Button>
      </Space>

      <Modal
        title={getOperationTitle()}
        open={operation !== null}
        onOk={handleOperation}
        onCancel={() => {
          setOperation(null);
          setResult(null);
          setSelectedRoles([]);
          setLockoutEnd(null);
        }}
        confirmLoading={loading}
        okText={t('bulkOperations.confirm')}
        okButtonProps={{ danger: operation === "delete" }}
        width={600}
      >
        {renderOperationContent()}

        {result && (
          <div style={{ marginTop: 16 }}>
            <Alert
              message={result.success ? t('bulkOperations.operationCompleted') : t('bulkOperations.operationCompletedWithErrors')}
              description={
                <div>
                  <p>{result.message}</p>
                  <p>
                    {t('bulkOperations.successCount', { successCount: result.successCount, totalCount: result.totalCount })}
                  </p>
                  {result.errors.length > 0 && (
                    <div style={{ marginTop: 8 }}>
                      <strong>{t('bulkOperations.errors')}</strong>
                      <Table
                        size="small"
                        dataSource={result.errors}
                        pagination={false}
                        columns={[
                          { title: t('bulkOperations.userColumn'), dataIndex: "userIdentifier", key: "user" },
                          { title: t('bulkOperations.errorColumn'), dataIndex: "errorMessage", key: "error" },
                        ]}
                      />
                    </div>
                  )}
                </div>
              }
              type={result.success ? "success" : "warning"}
              showIcon
            />
          </div>
        )}
      </Modal>
    </div>
  );
}

