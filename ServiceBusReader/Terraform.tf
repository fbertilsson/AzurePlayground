resource "azurerm_resource_group" "rg" {
  name     = "servicebusreader-rg"
  location = "West Europe"
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = "fbservicebusreader"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"

  tags = {
    source = "terraform"
  }
}

resource "azurerm_servicebus_topic" "topic1" {
  name                = "topic1"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name

  enable_partitioning = true
}

resource "azurerm_servicebus_subscription" "sub-pande" {
  name                = "sub-pande"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name
  topic_name          = azurerm_servicebus_topic.topic1.name
  max_delivery_count  = 6
}

resource "azurerm_servicebus_subscription_rule" "rule-pande" {
  name                = "rule-pande"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name
  topic_name          = azurerm_servicebus_topic.topic1.name
  subscription_name   = azurerm_servicebus_subscription.sub-pande.name
  filter_type         = "SqlFilter"
  sql_filter          = "destination = 'pande'"
}

resource "azurerm_servicebus_subscription" "sub-preview" {
  name                = "sub-preview"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name
  topic_name          = azurerm_servicebus_topic.topic1.name
  max_delivery_count  = 6
}

resource "azurerm_servicebus_subscription_rule" "rule-preview" {
  name                = "rule-preview"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.sb.name
  topic_name          = azurerm_servicebus_topic.topic1.name
  subscription_name   = azurerm_servicebus_subscription.sub-preview.name
  filter_type         = "SqlFilter"
  sql_filter          = "destination = 'preview'"
}