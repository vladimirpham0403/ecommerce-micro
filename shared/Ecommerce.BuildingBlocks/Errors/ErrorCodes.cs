namespace Ecommerce.BuildingBlocks.Errors;

public static class ErrorCodes
{
    // Auth
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string AuthRefreshTokenInvalid = "AUTH_REFRESH_TOKEN_INVALID";
    public const string AuthForbidden = "AUTH_FORBIDDEN";

    // Product
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductVariantNotFound = "PRODUCT_VARIANT_NOT_FOUND";
    public const string ProductInactive = "PRODUCT_INACTIVE";

    // Brands
    public const string BrandNotFound = "BRAND_NOT_FOUND";
    public const string BrandInactive = "PRODUCT_INACTIVE";

    // Category
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string CategoryInactive = "CATEGORY_INACTIVE";

    // Search
    public const string SearchQueryInvalid = "SEARCH_QUERY_INVALID";

    // Cart
    public const string CartEmpty = "CART_EMPTY";
    public const string CartItemNotFound = "CART_ITEM_NOT_FOUND";
    public const string CartInvalidQuantity = "CART_INVALID_QUANTITY";

    // Promotion
    public const string PromoCouponInvalid = "PROMO_COUPON_INVALID";
    public const string PromoCouponExpired = "PROMO_COUPON_EXPIRED";
    public const string PromoCouponExhausted = "PROMO_COUPON_EXHAUSTED";

    // Order
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderInvalidStatus = "ORDER_INVALID_STATUS";
    public const string OrderAlreadyCancelled = "ORDER_ALREADY_CANCELLED";
    public const string OrderDuplicatedRequest = "ORDER_DUPLICATED_REQUEST";

    // Inventory
    public const string InventoryNotEnoughStock = "INVENTORY_NOT_ENOUGH_STOCK";
    public const string InventoryReservationExpired = "INVENTORY_RESERVATION_EXPIRED";

    // Payment
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string PaymentTimeout = "PAYMENT_TIMEOUT";
    public const string PaymentAlreadyProcessed = "PAYMENT_ALREADY_PROCESSED";

    // Broker
    public const string BrokerPublishFailed = "BROKER_PUBLISH_FAILED";
    public const string BrokerConsumeFailed = "BROKER_CONSUME_FAILED";

    // Generic / system
    public const string ValidationError = "VALIDATION_ERROR";
    public const string SystemInternalError = "SYSTEM_INTERNAL_ERROR";
    public const string SystemTimeout = "SYSTEM_TIMEOUT";
    public const string SystemServiceUnavailable = "SYSTEM_SERVICE_UNAVAILABLE";
}
