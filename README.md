# Smart expense tracker service

A simple web application to help you track and manage your personal expenses. The app allows users to add, edit, and delete expense entries, categorize them, view summaries, and get insights into their spending habits through data visualizations.

## Features

- **Add, Edit, and Delete Expenses**: Add new expenses with details such as amount, date, description, and category. Edit or delete them as needed.
- **Expense Categorization**: Categorize expenses (e.g., food, travel, utilities) to easily track where your money is going.
- **Summary by Category**: View a summary of your expenses by category, with the total amounts spent in each category.
- **Search Functionality**: Search through your expenses using keywords in the description, category, or other details.
- **Data Visualizations**: Visualize your expenses with charts (e.g., pie charts, bar charts) to better understand your spending patterns.
- **Responsive UI**: A user-friendly, responsive front-end design to work seamlessly across all devices.


## Installation

### Prerequisites

- Docker


### Steps

1. **Clone the repository**:
   
 ```shell  
bash
   git clone https://github.com/patelveep/smart-expense-tracker-service
   cd smart-expense-tracker-service
```

2. Docker must be up and running

3. Execute the below commands
    3.a docker compose build
    3.b docker compose up

4. Backend service will be running on ```http://localhost:5000```

5. API documentation is available on ```http://localhost:5000/swagger/index.html```
