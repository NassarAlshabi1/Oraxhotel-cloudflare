const { Client, Databases } = require("node-appwrite");

const endpoint = "https://fra.cloud.appwrite.io/v1";
const projectId = "6a2b01d0000752ce97e7";
const apiKey = '';
const databaseId = "hotel_db";
const collectionId = "payments";

const client = new Client().setEndpoint(endpoint).setProject(projectId).setKey(apiKey);
const db = new Databases(client);

async function add(createFn, key, ...args) {
  try {
    await createFn(databaseId, collectionId, key, ...args);
    console.log(`✔ Created: ${key}`);
  } catch (e) {
    if (e?.code === 409) {
      console.log(`⚠ Already exists: ${key}`);
    } else {
      console.error(`✖ ${key}:`, e.message || e);
    }
  }
}

(async () => {
  await add(db.createStringAttribute, "voidReason", 500, false);
  await add(db.createBooleanAttribute, "isImmutable", false, false);
  await add(db.createStringAttribute, "createdAtIso", 50, false);
  await add(db.createStringAttribute, "updatedAtIso", 50, false);
  await add(db.createStringAttribute, "deletedAtIso", 50, false);
  await add(db.createIntegerAttribute, "createdAtEpoch", false);
  await add(db.createIntegerAttribute, "lastModifiedEpoch", false);
  await add(db.createIntegerAttribute, "version", false, 1);
  await add(db.createStringAttribute, "origin", 64, false);
  await add(db.createStringAttribute, "vectorClock", 64, false);
  await add(db.createStringAttribute, "deviceId", 100, false);
  await add(db.createIntegerAttribute, "syncTimestamp", false);
  await add(db.createStringAttribute, "sync_origin", 64, false);
  await add(db.createStringAttribute, "idempotencyKey", 255, false);
  console.log("\n✓ Done.");
})().catch((e) => {
  console.error("Fatal:", e);
  process.exit(1);
});
