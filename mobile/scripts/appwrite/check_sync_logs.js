#!/usr/bin/env node

const { Client, Databases } = require('node-appwrite');

const client = new Client()
    .setEndpoint('https://fra.cloud.appwrite.io/v1')
    .setProject('690ff0da0025518570c1')
    .setKey(process.env.APPWRITE_API_KEY || '');

const databases = new Databases(client);

async function checkSyncLogs() {
    try {
        console.log('🔍 فحص سجلات المزامنة الجديدة...\n');
        
        const logs = await databases.listDocuments('hotel_db', 'sync_logs');
        
        console.log(`📊 إجمالي السجلات: ${logs.total}\n`);
        
        if (logs.total > 0) {
            logs.documents.forEach((log, i) => {
                console.log(`${i + 1}. Sync Log:`);
                console.log(`   Device ID: ${log.deviceId || 'N/A'}`);
                console.log(`   Status: ${log.status || 'N/A'}`);
                console.log(`   Start: ${log.startTime || 'N/A'}`);
                console.log(`   End: ${log.endTime || 'N/A'}`);
                console.log(`   Duration: ${log.durationMs || 0}ms`);
                console.log(`   Uploaded: ${log.changesUploaded || 0}`);
                console.log(`   Downloaded: ${log.changesDownloaded || 0}`);
                if (log.errors) console.log(`   Errors: ${log.errors}`);
                console.log('');
            });
        } else {
            console.log('⚪ لا توجد سجلات مزامنة');
        }
        
    } catch (error) {
        console.error('❌ Error:', error.message);
    }
}

checkSyncLogs();
