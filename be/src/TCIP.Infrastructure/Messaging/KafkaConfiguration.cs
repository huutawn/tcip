using Microsoft.Extensions.Configuration;
using TCIP.Business.Modules.Calendar.Domain.Models;

namespace TCIP.Infrastructure.Messaging;

public static class KafkaConfiguration
{
    public static bool Enabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Kafka:Enabled");

    public static string BootstrapServers(IConfiguration configuration) =>
        configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException("Kafka:BootstrapServers is required.");

    public static string ReminderDueTopic(IConfiguration configuration) =>
        configuration["Kafka:ReminderDueTopic"] ?? CalendarKafkaTopics.ReminderDueTopicV2;

    public static string NotificationBatchTopic(IConfiguration configuration) =>
        configuration["Kafka:NotificationBatchTopic"] ?? CalendarKafkaTopics.NotificationBatchTopicV1;

    public static string AudienceResolverGroup(IConfiguration configuration) =>
        configuration["Kafka:AudienceResolverGroup"] ?? CalendarKafkaTopics.AudienceResolverGroupV2;

    public static string NotificationBatchDeliveryGroup(IConfiguration configuration) =>
        configuration["Kafka:NotificationBatchDeliveryGroup"] ?? CalendarKafkaTopics.NotificationBatchDeliveryGroupV1;
}
