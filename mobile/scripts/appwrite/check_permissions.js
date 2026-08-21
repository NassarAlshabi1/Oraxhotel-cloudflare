#!/usr/bin/env node

const { Client, Databases } = require('node-appwrite');

const client = new Client()
    .setEndpoint('https://fra.cloud.appwrite.io/v1')
    .setProject('690ff0da0025518570c1')
    .setKey(process.env.APPWRITE_API_KEY || '');

const databases = new Databases(client);

async function checkPermissions() {
    try {
        console.log('🔍 فحص صلاحيات Collections...\n');
        
        const collections = await databases.listCollections('hotel_db');
        
        for (const col of collections.collections) {
            console.log(`📁 ${col.name} (${col.$id})`);
            console.log(`   Document Security: ${col.documentSecurity}`);
            console.log(`   Enabled: ${col.enabled}`);
            
            // Get one document to check permissions
            try {
                const docs = await databases.listDocuments('hotel_db', col.$id);
                if (docs.total > 0) {
                    const firstDoc = docs.documents[0];
                    console.log(`   Document Permissions: ${JSON.stringify(firstDoc.$permissions || [])}`);
                }
            } catch (e) {
                console.log(`   Cannot check document permissions: ${e.message}`);
            }
            console.log('');
        }
        
    } catch (error) {
        console.error('❌ Error:', error.message);
    }
}

checkPermissions();
