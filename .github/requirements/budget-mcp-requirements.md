Please update this project through create new c# code in the GitLabTools file that will offer new MCP Tools for the LLM:

- looks through a specific gitlab project for the existence of a terraform azurerm_consumption_budget_subscription resource. 

if it doesn't find this resource type, it must perform the following steps programmatically: 

1 - clone the gitlab project
2 - prompt the user for which branch to checkout and pull from initially
3 - create a new branch called "feat/platform-engineering/add-budget", 
4 - add this terraform resource to a file called budget.tf:

```hcl
resource "azurerm_consumption_budget_subscription" "this" {
  name            = "budget"
  subscription_id = "/subscriptions/${var.ARM_SUBSCRIPTION_ID}"
  amount          = var.budget_amount
  time_grain      = "Monthly"
  time_period {
    start_date = "2025-05-01T00:00:00Z"
    end_date   = "2026-05-01T00:00:00Z"
  }

  notification {
    enabled        = true
    threshold      = 80
    operator       = "GreaterThan"
    contact_emails = [var.budget_notification_email]
  }
}
```

- the time_period.start_date above must be today, and time_period.end_date must be a year from today

5 - It must then add these variables to the variable.tf file:

```hcl
variable "budget_amount" {
  description = "The allocated budget for this subscription"
  type        = number
  default     = 100
}

variable "budget_notification_email" {
  description = "The address to use to notify when the budet hits the threshold and beyond"
  type        = string
  default     = "garrard.kitchen@fujitsu.com"
}
``

6 - Add these outputs to the output.tf file:

```hcl
output "budget_name" {
  value = azurerm_consumption_budget_subscription.example.name
}
```

7 - create a Merge Request

8 - render back to the user, the url of the merge request

9 - any http calls, should presuffix any string interpolation with "https://"

10 - when cloning the gitlab repository, we must use https and not ssh