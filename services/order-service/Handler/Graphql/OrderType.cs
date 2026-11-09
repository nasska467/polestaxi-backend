using HotChocolate.Types;
using PolesTaxi.OrderService.Entity.Service;

namespace PolesTaxi.OrderService.Handler.Graphql;

public sealed class OrderType : ObjectType<Order>
{
    protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor.Field(x => x.Id).Type<NonNullType<StringType>>();
        descriptor.Field(x => x.Status).Type<StringType>();
        descriptor.Field(x => x.Price).Type<FloatType>();
        descriptor.Field(x => x.DistanceMeters).Type<LongType>();
    }
}