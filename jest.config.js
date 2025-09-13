module.exports = {
  preset: 'jest-puppeteer',
  testMatch: ['**/tests/**/*.test.js'],
  testTimeout: 30000,
  setupFilesAfterEnv: ['<rootDir>/tests/setup.js'],
  collectCoverageFrom: [
    'wwwroot/js/**/*.js',
    '!wwwroot/js/**/*.min.js'
  ],
  coverageDirectory: 'reports/coverage',
  coverageReporters: ['text', 'lcov', 'html'],
  reporters: [
    'default',
    ['jest-html-reporters', {
      publicPath: './reports/jest',
      filename: 'test-report.html',
      expand: true
    }]
  ]
};
