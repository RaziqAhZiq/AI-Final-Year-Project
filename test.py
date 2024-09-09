import numpy as np
import matplotlib.pyplot as plt

# Generate some sample data for linear regression
np.random.seed(42)
X = 2 * np.random.rand(100, 1)
Y = 4 + 3 * X + np.random.randn(100, 1)

# Simple Linear Regression line (manual calculation for demo purposes)
X_new = np.array([[0], [2]])
Y_predict = 4 + 3 * X_new

# Plotting the data and the regression line
plt.figure(figsize=(10, 6))
plt.scatter(X, Y, color='blue', label="Data points")
plt.plot(X_new, Y_predict, color='red', linewidth=2, label="Regression Line")
plt.title("Simple Linear Regression Example")
plt.xlabel("Input Feature (X)")
plt.ylabel("Output (Y)")
plt.legend()
plt.grid(True)
plt.show()

# Creating a simple illustration of linear regression as a layer in a neural network
plt.figure(figsize=(10, 6))
plt.title("Linear Regression as a Layer in a Neural Network")

# Plotting input features
for i in range(1, 4):
    plt.scatter([1], [10*i], color='blue', s=300)
    plt.text(1.1, 10*i-1, f"X{i}", fontsize=15, verticalalignment='center')

# Plotting output node
plt.scatter([5], [20], color='green', s=500)
plt.text(5.1, 19, "Y (Prediction)", fontsize=15, verticalalignment='center')

# Drawing lines (representing weights) from input features to output
for i in range(1, 4):
    plt.plot([1, 5], [10*i, 20], color='gray', linewidth=2)

# Adding weights on the lines
for i in range(1, 4):
    plt.text(3, 10*i + (20-10*i)/2, f"W{i}", fontsize=12, color='red', verticalalignment='center')

# Adding bias
plt.text(3.5, 21, "+ Bias", fontsize=15, color='orange', verticalalignment='center')

plt.xlim(0, 6)
plt.ylim(0, 30)
plt.axis('off')
plt.show()
