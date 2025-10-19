import { useEffect } from "react";
import { userManager } from "../lib/oidcClient";
import { Loading } from "../components/Loading";

export function SignInPage() {
  useEffect(() => {
    async function handleSignIn() {
      try {
        // 这里直接发起OIDC授权流程
        await userManager.signinRedirect();
      } catch (error) {
        console.error("Signin failed", error);
      }
    }

    handleSignIn();
  }, []);

  return <Loading />;
}
