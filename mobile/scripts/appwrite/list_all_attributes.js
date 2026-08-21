const { Client, Databases, Query } = require("node-appwrite");

const endpoint = "https://fra.cloud.appwrite.io/v1";
const project = "6a2b01d0000752ce97e7";
const apiKey = '';
const database = "6a2b030d000445596163";

const client = new Client().setEndpoint(endpoint).setProject(project).setKey(apiKey);
const db = new Databases(client);

async function listAll(col) {
  const keys = new Set();
  let offset = 0;
  while (true) {
    try {
      const res = await db.listAttributes(database, col, [
        Query.limit(100),
        Query.offset(offset),
      ]);
      const attrs = res.attributes.map(a => a.key);
      attrs.forEach(k => keys.add(k));
      console.log(`  fetched ${attrs.length} (offset=${offset})`);
      if (attrs.length < 100) break;
      offset += attrs.length;
    } catch (e) {
      console.log(`  error at offset ${offset}: ${e.message || e}`);
      break;
    }
  }
  return keys;
}

(async () => {
  const cols = ["rooms","bookings","payments","expenses","employees","debts","booking_notes","booking_nights","cash_transactions","shift_notes","salary_cycles","salary_payments","salary_withdrawals","salary_carry_over_logs","price_adjustments","booking_price_adjustments","audit_logs","payment_voids","guest_infos","blacklist","devices","sync_logs","app_settings","sync_state","app_users"];
  for (const col of cols) {
    console.log(`=== ${col} ===`);
    const keys = await listAll(col);
    console.log(`Total ${col}: ${keys.size}`);
    console.log(JSON.stringify([...keys].sort()));
  }
})();
