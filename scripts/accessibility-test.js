#!/usr/bin/env node

const puppeteer = require('puppeteer');
const axe = require('axe-core');
const fs = require('fs');
const path = require('path');

// Configuration
const BASE_URL = process.env.BASE_URL || 'http://localhost:5000';
const CI_MODE = process.argv.includes('--ci');
const REPORTS_DIR = path.join(__dirname, '..', 'reports');

// Ensure reports directory exists
if (!fs.existsSync(REPORTS_DIR)) {
    fs.mkdirSync(REPORTS_DIR, { recursive: true });
}

// Pages to test
const PAGES_TO_TEST = [
    '/',
    '/Products',
    '/Categories',
    '/About',
    '/Contact',
    '/Data'
];

class AccessibilityTester {
    constructor() {
        this.browser = null;
        this.page = null;
        this.results = [];
        this.startTime = Date.now();
    }

    async init() {
        console.log('🔍 Initializing accessibility testing...');
        this.browser = await puppeteer.launch({
            headless: CI_MODE ? 'new' : false,
            args: ['--no-sandbox', '--disable-setuid-sandbox']
        });
        this.page = await this.browser.newPage();
        
        // Set viewport for consistent testing
        await this.page.setViewport({ width: 1280, height: 720 });
        
        // Inject axe-core
        await this.page.addScriptTag({
            url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.8.4/axe.min.js'
        });
    }

    async testPage(url) {
        console.log(`📄 Testing: ${url}`);
        
        try {
            const fullUrl = `${BASE_URL}${url}`;
            await this.page.goto(fullUrl, { 
                waitUntil: 'networkidle2',
                timeout: 30000 
            });

            // Wait for page to be fully loaded
            await this.page.waitForTimeout(2000);

            // Run axe accessibility tests
            const results = await this.page.evaluate(() => {
                return new Promise((resolve) => {
                    axe.run(document, {
                        tags: ['wcag2a', 'wcag2aa', 'wcag21aa'],
                        rules: {
                            // Enable all rules
                        }
                    }, (err, results) => {
                        if (err) {
                            resolve({ error: err.toString() });
                        } else {
                            resolve(results);
                        }
                    });
                });
            });

            const pageResult = {
                url: url,
                fullUrl: fullUrl,
                timestamp: new Date().toISOString(),
                violations: results.violations || [],
                passes: results.passes || [],
                incomplete: results.incomplete || [],
                inapplicable: results.inapplicable || []
            };

            this.results.push(pageResult);
            
            // Log summary for this page
            const violationCount = pageResult.violations.length;
            const passCount = pageResult.passes.length;
            
            if (violationCount === 0) {
                console.log(`✅ ${url}: ${passCount} tests passed, 0 violations`);
            } else {
                console.log(`❌ ${url}: ${passCount} tests passed, ${violationCount} violations`);
                
                // Log violations in detail
                pageResult.violations.forEach(violation => {
                    console.log(`   🚨 ${violation.id}: ${violation.description}`);
                    violation.nodes.forEach(node => {
                        console.log(`      - ${node.html.substring(0, 100)}...`);
                    });
                });
            }

        } catch (error) {
            console.error(`❌ Error testing ${url}:`, error.message);
            this.results.push({
                url: url,
                fullUrl: `${BASE_URL}${url}`,
                timestamp: new Date().toISOString(),
                error: error.message,
                violations: [],
                passes: [],
                incomplete: [],
                inapplicable: []
            });
        }
    }

    async runTests() {
        console.log('🚀 Starting accessibility tests...');
        console.log(`📍 Base URL: ${BASE_URL}`);
        console.log(`📋 Pages to test: ${PAGES_TO_TEST.length}`);
        
        for (const page of PAGES_TO_TEST) {
            await this.testPage(page);
        }
        
        await this.generateReport();
    }

    async generateReport() {
        const endTime = Date.now();
        const duration = (endTime - this.startTime) / 1000;
        
        const summary = {
            timestamp: new Date().toISOString(),
            duration: `${duration}s`,
            baseUrl: BASE_URL,
            totalPages: this.results.length,
            totalViolations: this.results.reduce((sum, result) => sum + result.violations.length, 0),
            totalPasses: this.results.reduce((sum, result) => sum + result.passes.length, 0),
            pagesWithViolations: this.results.filter(result => result.violations.length > 0).length,
            pagesWithoutViolations: this.results.filter(result => result.violations.length === 0).length
        };

        // Generate JSON report
        const jsonReport = {
            summary,
            results: this.results
        };

        const jsonReportPath = path.join(REPORTS_DIR, 'accessibility-results.json');
        fs.writeFileSync(jsonReportPath, JSON.stringify(jsonReport, null, 2));

        // Generate HTML report
        const htmlReport = this.generateHtmlReport(jsonReport);
        const htmlReportPath = path.join(REPORTS_DIR, 'accessibility-report.html');
        fs.writeFileSync(htmlReportPath, htmlReport);

        // Print summary
        console.log('\n📊 Accessibility Test Summary:');
        console.log(`⏱️  Duration: ${summary.duration}`);
        console.log(`📄 Pages tested: ${summary.totalPages}`);
        console.log(`✅ Pages without violations: ${summary.pagesWithoutViolations}`);
        console.log(`❌ Pages with violations: ${summary.pagesWithViolations}`);
        console.log(`🚨 Total violations: ${summary.totalViolations}`);
        console.log(`✅ Total passes: ${summary.totalPasses}`);
        
        console.log('\n📁 Reports generated:');
        console.log(`   📄 JSON: ${jsonReportPath}`);
        console.log(`   🌐 HTML: ${htmlReportPath}`);

        // Exit with error code if violations found (for CI)
        if (CI_MODE && summary.totalViolations > 0) {
            console.log('\n❌ Accessibility violations found. Build failed.');
            process.exit(1);
        } else if (summary.totalViolations === 0) {
            console.log('\n✅ All accessibility tests passed!');
        }
    }

    generateHtmlReport(data) {
        const { summary, results } = data;
        
        return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>FIPS Accessibility Test Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 20px;
            background-color: #f8f9fa;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        .header {
            background: ${summary.totalViolations === 0 ? '#28a745' : '#dc3545'};
            color: white;
            padding: 20px;
            text-align: center;
        }
        .summary {
            padding: 20px;
            border-bottom: 1px solid #dee2e6;
        }
        .summary-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        .summary-card {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 6px;
            text-align: center;
        }
        .summary-card h3 {
            margin: 0 0 10px 0;
            color: #495057;
        }
        .summary-card .value {
            font-size: 2em;
            font-weight: bold;
            color: ${summary.totalViolations === 0 ? '#28a745' : '#dc3545'};
        }
        .page-results {
            padding: 20px;
        }
        .page-result {
            border: 1px solid #dee2e6;
            border-radius: 6px;
            margin-bottom: 20px;
            overflow: hidden;
        }
        .page-header {
            background: #f8f9fa;
            padding: 15px;
            border-bottom: 1px solid #dee2e6;
        }
        .page-header h3 {
            margin: 0;
            color: #495057;
        }
        .violation {
            background: #fff5f5;
            border-left: 4px solid #dc3545;
            padding: 15px;
            margin: 10px;
        }
        .violation h4 {
            margin: 0 0 10px 0;
            color: #dc3545;
        }
        .violation-code {
            background: #e9ecef;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: monospace;
            font-size: 0.9em;
        }
        .node {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 4px;
            padding: 10px;
            margin: 10px 0;
            font-family: monospace;
            font-size: 0.9em;
            overflow-x: auto;
        }
        .timestamp {
            color: #6c757d;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>FIPS Accessibility Test Report</h1>
            <p class="timestamp">Generated: ${new Date(summary.timestamp).toLocaleString()}</p>
        </div>
        
        <div class="summary">
            <h2>Summary</h2>
            <div class="summary-grid">
                <div class="summary-card">
                    <h3>Pages Tested</h3>
                    <div class="value">${summary.totalPages}</div>
                </div>
                <div class="summary-card">
                    <h3>Violations</h3>
                    <div class="value">${summary.totalViolations}</div>
                </div>
                <div class="summary-card">
                    <h3>Tests Passed</h3>
                    <div class="value">${summary.totalPasses}</div>
                </div>
                <div class="summary-card">
                    <h3>Success Rate</h3>
                    <div class="value">${Math.round((summary.pagesWithoutViolations / summary.totalPages) * 100)}%</div>
                </div>
            </div>
        </div>
        
        <div class="page-results">
            <h2>Page Results</h2>
            ${results.map(result => `
                <div class="page-result">
                    <div class="page-header">
                        <h3>${result.url}</h3>
                        <p class="timestamp">${new Date(result.timestamp).toLocaleString()}</p>
                    </div>
                    ${result.violations.length > 0 ? `
                        ${result.violations.map(violation => `
                            <div class="violation">
                                <h4>${violation.description}</h4>
                                <p><span class="violation-code">${violation.id}</span></p>
                                <p>Impact: ${violation.impact}</p>
                                <p>Help: ${violation.helpUrl}</p>
                                ${violation.nodes.map(node => `
                                    <div class="node">${node.html}</div>
                                `).join('')}
                            </div>
                        `).join('')}
                    ` : `
                        <div style="padding: 20px; text-align: center; color: #28a745;">
                            ✅ No accessibility violations found
                        </div>
                    `}
                </div>
            `).join('')}
        </div>
    </div>
</body>
</html>`;
    }

    async cleanup() {
        if (this.browser) {
            await this.browser.close();
        }
    }
}

// Main execution
async function main() {
    const tester = new AccessibilityTester();
    
    try {
        await tester.init();
        await tester.runTests();
    } catch (error) {
        console.error('❌ Test execution failed:', error);
        process.exit(1);
    } finally {
        await tester.cleanup();
    }
}

// Run if called directly
if (require.main === module) {
    main();
}

module.exports = AccessibilityTester;
