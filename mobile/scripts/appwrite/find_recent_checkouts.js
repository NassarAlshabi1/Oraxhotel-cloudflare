#!/usr/bin/env node
/**
 * سكربت: البحث عن آخر الحجوزات التي تم تسجيل خروجها
 * لمعرفة الحجز الذي تريد إرجاعه
 */
const { Client, Databases, Query } = require('node-appwrite');

const ENDPOINT = 'https://fra.cloud.appwrite.io/v1';
const PROJECT_ID = '690ff0da0025518570c1';
const DATABASE_ID = 'hotel_db';
const API_KEY = process.env.APPWRITE_API_KEY || '';

async function main() {
  const client = new Client().setEndpoint(ENDPOINT).setProject(PROJECT_ID).setKey(API_KEY);
  const db = new Databases(client);

  console.log('🔍 البحث عن آخر الحجوزات بم حالة "مكتمل" (تم تسجيل الخروج)...\n');

  // جلب آخر 20 حجز مكتمل
  const result = await db.listDocuments(DATABASE_ID, 'bookings', [
    Query.equal('status', 'مكتمل'),
    Query.orderDesc('updatedAt'),
    Query.limit(20),
  ]);

  if (result.documents.length === 0) {
    console.log('لم يتم العثور على حجوزات مكتملة.');
    return;
  }

  console.log(`تم العثور على ${result.documents.length} حجز مكتمل:\n`);
  console.log('─'.repeat(100));

  for (const b of result.documents) {
    const name = b.guestName || 'بدون اسم';
    const room = b.roomNumber || '?';
    const checkin = b.checkinDate || '?';
    const checkout = b.actualCheckout || b.checkoutDate || '?';
    const nights = b.calculatedNights || '?';
    const total = b.totalDueCached || 0;
    const updated = b.updatedAtIso || '';
    const uuid = b.localUuid || '';
    const docId = b.$id || '';

    console.log(`📝 اسم النزيل: ${name}`);
    console.log(`🏠 رقم الغرفة: ${room}`);
    console.log(`📅 تاريخ الدخول: ${checkin}`);
    console.log(`🚪 تاريخ الخروج: ${checkout}`);
    console.log(`🌙 عدد الليالي: ${nights}`);
    console.log(`💰 المبلغ الإجمالي: ${total}`);
    console.log(`🕐 آخر تحديث: ${updated}`);
    console.log(`🆔 Document ID: ${docId}`);
    console.log(`🔗 localUuid: ${uuid}`);
    console.log('─'.repeat(100));
  }
}

main().catch(err => { console.error('❌ خطأ:', err.message); process.exit(1); });
