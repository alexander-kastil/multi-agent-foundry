"use client";

import { CopilotSidebar } from "@copilotkit/react-core/v2";

export default function Page() {
  return (
    <main>
      <h1>Contoso support desk</h1>
      <CopilotSidebar agentId="support_desk" defaultOpen />
    </main>
  );
}
