# Enhanced Caching System

This document describes the comprehensive caching system implemented in the FIPS frontend application.

## Overview

The enhanced caching system provides multiple layers of caching with intelligent strategies, performance monitoring, and management capabilities. It supports both in-memory and distributed caching with Redis.

## Architecture

### Core Components

1. **CacheConfigurationService** - Centralized cache configuration and strategy management
2. **EnhancedCacheService** - Multi-tier caching with memory and distributed support
3. **CacheWarmingService** - Pre-loads critical data for optimal performance
4. **CacheInvalidationService** - Smart cache invalidation patterns
5. **CacheController** - Management UI and API endpoints

### Caching Strategies

The system supports four caching strategies:

- **MemoryOnly**: Uses only in-memory cache (fastest)
- **DistributedOnly**: Uses only distributed cache (Redis)
- **MemoryWithDistributedFallback**: Memory first, then distributed
- **NoCache**: Disables caching for specific operations

## Configuration

### appsettings.json
```json
{
  "Caching": {
    "DefaultDurationMinutes": 5,
    "Redis": {
      "Enabled": false,
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "fips:"
    },
    "MemoryCache": {
      "SizeLimit": 100,
      "CompactionPercentage": 0.25
    }
  }
}
```

### Cache Durations by Content Type

- **Home page data**: 5 minutes
- **Product data**: 5-10 minutes
- **Category data**: 15 minutes
- **Search results**: 2 minutes
- **Static content**: 1 hour
- **Admin operations**: No caching
- **Health checks**: 30 seconds

## Features

### 1. Multi-Tier Caching
- **Memory Cache**: Fastest access for frequently used data
- **Distributed Cache**: Shared across multiple instances (Redis)
- **Response Cache**: HTTP-level caching for entire responses

### 2. Cache Warming
Automatically pre-loads critical data:
- Product lists and counts
- Category types and values
- Home page statistics
- Filter options

### 3. Smart Invalidation
Intelligent cache invalidation based on data changes:
- Product-specific invalidation
- Category-specific invalidation
- Pattern-based invalidation
- Bulk invalidation

### 4. Performance Monitoring
Real-time cache performance metrics:
- Hit/miss ratios
- Memory usage
- Response times
- Cache efficiency

### 5. Management Interface
Comprehensive cache management UI:
- View cache statistics
- Clear specific caches
- Warm cache manually
- Monitor performance

## Usage Examples

### Basic Caching
```csharp
// Get data with automatic caching
var products = await _enhancedCacheService.GetAsync<List<Product>>("products");

// Set data with custom duration
await _enhancedCacheService.SetAsync("products", products, TimeSpan.FromMinutes(10));
```

### Cache Warming
```csharp
// Warm critical data
await _cacheWarmingService.WarmCriticalDataAsync();

// Check if data is warmed
var isWarmed = await _cacheWarmingService.IsDataWarmedAsync("products");
```

### Cache Invalidation
```csharp
// Invalidate specific product
await _cacheInvalidationService.InvalidateProductCacheAsync(productId);

// Invalidate all product cache
await _cacheInvalidationService.InvalidateProductCacheAsync();

// Invalidate by pattern
await _cacheInvalidationService.InvalidateByPatternAsync("products_*");
```

## API Endpoints

### Cache Management
- `GET /Cache` - Cache management interface
- `POST /Cache/ClearAllCache` - Clear all caches
- `POST /Cache/ClearProductCache` - Clear product cache
- `POST /Cache/ClearCategoryCache` - Clear category cache
- `POST /Cache/WarmCache` - Warm cache manually
- `GET /Cache/CacheStats` - Get cache statistics

### Cache Statistics Response
```json
{
  "TotalEntries": 25,
  "MemoryHits": 150,
  "DistributedHits": 45,
  "Misses": 20,
  "TotalHits": 195,
  "HitRate": 90.7,
  "MemoryUsage": 25600
}
```

## Redis Configuration

### Enable Redis
1. Set `Caching:Redis:Enabled` to `true`
2. Configure Redis connection string
3. Set appropriate key prefix
4. Restart application

### Redis Benefits
- **Scalability**: Shared cache across multiple instances
- **Persistence**: Survives application restarts
- **Performance**: Fast key-value storage
- **Clustering**: Support for Redis clusters

## Performance Optimization

### Cache Hit Optimization
- **Preloading**: Critical data warmed on startup
- **Smart Keys**: Hierarchical cache key structure
- **TTL Management**: Appropriate expiration times
- **Compression**: Efficient memory usage

### Memory Management
- **Size Limits**: Configurable memory cache limits
- **Compaction**: Automatic cleanup of expired entries
- **Monitoring**: Real-time memory usage tracking
- **Eviction**: LRU-based eviction policies

## Monitoring and Debugging

### Cache Statistics
The system provides comprehensive statistics:
- Total cache entries
- Hit/miss ratios
- Memory usage
- Performance metrics

### Logging
Detailed logging for:
- Cache operations
- Performance metrics
- Error conditions
- Configuration changes

### Health Checks
Built-in health monitoring:
- Cache availability
- Performance thresholds
- Memory usage alerts
- Redis connectivity

## Best Practices

### 1. Cache Key Design
- Use hierarchical keys: `products_page_1`, `products_count`
- Include version information for breaking changes
- Use consistent naming conventions

### 2. TTL Management
- Set appropriate expiration times
- Use shorter TTLs for frequently changing data
- Implement cache warming for critical data

### 3. Invalidation Strategy
- Invalidate related data when updating
- Use pattern-based invalidation for bulk operations
- Consider cache-aside pattern for complex scenarios

### 4. Performance Monitoring
- Monitor hit/miss ratios regularly
- Set up alerts for performance degradation
- Track memory usage trends

## Troubleshooting

### Common Issues

1. **High Memory Usage**
   - Check cache size limits
   - Review TTL settings
   - Monitor cache hit ratios

2. **Poor Performance**
   - Verify cache warming
   - Check Redis connectivity
   - Review cache strategies

3. **Stale Data**
   - Verify invalidation logic
   - Check TTL settings
   - Review cache warming

### Debug Commands
```bash
# Check Redis connectivity
redis-cli ping

# View cache statistics
curl /Cache/CacheStats

# Clear specific cache
curl -X POST /Cache/ClearProductCache
```

## Migration Guide

### From Basic Caching
1. Update service registrations
2. Replace direct cache calls with enhanced service
3. Configure new cache settings
4. Test cache warming functionality

### Redis Migration
1. Install Redis server
2. Update configuration
3. Test distributed caching
4. Monitor performance

## Future Enhancements

- **Cache Compression**: Reduce memory usage
- **Predictive Warming**: ML-based cache warming
- **Advanced Analytics**: Detailed performance insights
- **Multi-Region**: Cross-region cache replication
