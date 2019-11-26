resource "azurerm_resource_group" "rg" {
  name     = "servicebusreader-rg"
  location = "West Europe"
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = "fbservicebusreader"
  location            = "${azurerm_resource_group.rg.location}"
  resource_group_name = "${azurerm_resource_group.rg.name}"
  sku                 = "Standard"

  tags = {
    source = "terraform"
  }
}

resource "azurerm_servicebus_topic" "topic1" {
  name                = "topic1"
  resource_group_name = "${azurerm_resource_group.rg.name}"
  namespace_name      = "${azurerm_servicebus_namespace.sb.name}"

  enable_partitioning = true
}

resource "azurerm_servicebus_subscription" "sub1" {
  name                = "sub1"
  resource_group_name = "${azurerm_resource_group.rg.name}"
  namespace_name      = "${azurerm_servicebus_namespace.sb.name}"
  topic_name          = "${azurerm_servicebus_topic.topic1.name}"
  max_delivery_count  = 5
}