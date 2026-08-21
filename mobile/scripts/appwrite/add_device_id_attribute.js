#!/usr/bin/env node

/**
 * سكربت: إضافة حقل deviceId إلى جميع مجموعات Appwrite Cloud
 * يجب تشغيله مرة واحدة بعد إضافة deviceId إلى SyncFields
 */

const { Client, Databases } = require('node-appwrite');

const ENDPOINT = 'https://fra.cloud.appwrite.io/v1';
const PROJECT_ID = '690ff0da0025518570c1';
const DATABASE_ID = 'hotel_db';
const API_KEY = process.env.APPWRITE_API_KEY || '';

// جميع المجموعات التي تستخدم SyncFields وتحتاج deviceId
const COLLECTIONS = [
  'rooms',
  'bookings',
  'booking_notes',
  'employees',
  'expenses',
  'cash_transactions',
  'payments',
  'debts',
  'shift_notes',
  'booking_nights',
  'hotel_day_ledger',
  'price_adjustments',
  'booking_price_adjustments',
  'payment_voids',
  'guest_infos',
  'salary_cycles',
  'salary_payments',
  'salary_withdrawals',
];

const dryRun = process.argv.includes('--dry-run');

async function main() {
  const client = new Client().setEndpoint(ENDPOINT).setProject(PROJECT_ID).setKey(API_KEY);
  const db = new Databases(client);

  console.log('='.repeat(60));
  console.log('إضافة حقل deviceId إلى مجموعات Appwrite Cloud');
  console.log('='.repeat(60));
  console.log(`Dry Run: ${dryRun ? 'نعم' : 'لا'}`);
  console.log(`عدد المجموعات: ${COLLECTIONS.length}`);
  console.log('');

  let added = 0;
  let skipped = 0;
  let failed = 0;

  for (const collectionId of COLLECTIONS) {
    try {
      // التحقق مما إذا كان الحقل موجوداً بالفعل
      const collection = await db.listAttributes(DATABASE_ID, collectionId);
      const existingAttr = collection.attributes?.find(a => a.key === 'deviceId');
      
      if (existingAttr) {
        console.log(`  ⏭️  ${collectionId}: deviceId موجود بالفعل (status: ${existingAttr.status})`);
        skipped++;
        continue;
      }

      if (dryRun) {
        console.log(`  ✅ ${collectionId}: سيتم إضافة deviceId (size: 64, default: '')`);
        added++;
        continue;
      }

      // إضافة الحقل
      await db.createStringAttribute(
        DATABASE_ID,
        collectionId,
        'deviceId',
        64,        // size
        false,     // required = false
        '',        // default = ''
      );
      console.log(`  ✅ ${collectionId}: تم إضافة deviceId`);
      added++;
    } catch (e) {
      if (e.message?.includes('already exists') || e.message?.includes('Attribute already exists')) {
        console.log(`  ⏭️  ${collectionId}: deviceId موجود بالفعل`);
        skipped++;
      } else {
        console.log(`  ❌ ${collectionId}: فشل - ${e.message}`);
        failed++;
      }
    }
  }

  console.log('\n' + '='.repeat(60));
  console.log('ملخص النتائج:');
  console.log(`  ✅ تم إضافة: ${added}`);
  console.log(`  ⏭️  موجود مسبقاً: ${skipped}`);
  console.log(`  ❌ فشل: ${failed}`);
  console.log('='.repeat(60));
  
  if (!dryRun && added > 0) {
    console.log('\n⏳ ملاحظة: Appwrite يحتاج بعض الوقت لمعالجة الحقول الجديدة.');
    console.log('   تحقق من حالة الحقول في لوحة تحكم Appwrite.');
  }
}

main().catch(err => { console.error('❌ خطأ عام:', err.message); process.exit(1); });
