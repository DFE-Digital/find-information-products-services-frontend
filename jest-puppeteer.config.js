module.exports = {
  launch: {
    headless: process.env.CI === 'true' ? 'new' : false,
    slowMo: process.env.CI === 'true' ? 0 : 100,
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-accelerated-2d-canvas',
      '--no-first-run',
      '--no-zygote',
      '--disable-gpu'
    ]
  },
  server: {
    command: 'dotnet run',
    port: 5000,
    launchTimeout: 30000,
    debug: true
  },
  browserContext: 'default'
};
