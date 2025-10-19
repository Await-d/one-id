import { createBrowserRouter } from "react-router-dom";
import { HomePage } from "./pages/Home";
import { CallbackPage } from "./pages/Callback";
import { SignInPage } from "./pages/SignIn";
import { LoginPage } from "./pages/Login";
import { ProfilePage } from "./pages/Profile";
import MfaSetup from "./pages/MfaSetup";
import RegisterPage from "./pages/Register";
import TwoFactorPage from "./pages/TwoFactor";
import ForgotPasswordPage from "./pages/ForgotPassword";
import ResetPasswordPage from "./pages/ResetPassword";
import ApiKeysPage from "./pages/ApiKeys";
import ConfirmEmailPage from "./pages/ConfirmEmail";
import ResendConfirmationPage from "./pages/ResendConfirmation";
import { Consent } from "./pages/Consent";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <HomePage />,
  },
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    path: "/signin",
    element: <SignInPage />,
  },
  {
    path: "/callback",
    element: <CallbackPage />,
  },
  {
    path: "/profile",
    element: <ProfilePage />,
  },
  {
    path: "/mfa-setup",
    element: <MfaSetup />,
  },
  {
    path: "/register",
    element: <RegisterPage />,
  },
  {
    path: "/two-factor",
    element: <TwoFactorPage />,
  },
  {
    path: "/forgot-password",
    element: <ForgotPasswordPage />,
  },
  {
    path: "/reset-password",
    element: <ResetPasswordPage />,
  },
  {
    path: "/api-keys",
    element: <ApiKeysPage />,
  },
  {
    path: "/confirm-email",
    element: <ConfirmEmailPage />,
  },
  {
    path: "/resend-confirmation",
    element: <ResendConfirmationPage />,
  },
  {
    path: "/consent",
    element: <Consent />,
  },
]);
