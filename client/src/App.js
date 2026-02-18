import React, { useEffect, useState } from 'react';
import service from './service.js';
import Login from './Login.js';

function App() {
  const [newTodo, setNewTodo] = useState("");
  const [todos, setTodos] = useState([]);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // בדיקה אם יש token בטעינה
    const token = localStorage.getItem("access_token");
    setIsAuthenticated(!!token);
    if (token) {
      getTodos();
    }
  }, []);

  async function getTodos() {
      try {
      const todos = await service.getTasks();
      setTodos(todos);
    } catch (error) {
      console.error("Failed to load tasks:", error);
      setIsAuthenticated(false); // אם נכשל - כנראה ה-token לא תקין
    }
  }

  async function createTodo(e) {
     try {
       e.preventDefault();
       if (!newTodo.trim()) return;
      await service.addTask(newTodo);
      setNewTodo("");//clear input
      await getTodos();//refresh tasks list (in order to see the new one)
    } catch (error) {
      console.error("Failed to create task:", error);
    }
  }

  async function updateCompleted(todo, isComplete) {
    await service.setCompleted(todo.id, isComplete);
    await getTodos();//refresh tasks list (in order to see the updated one)
  }

  async function deleteTodo(id) {
     try {
      await service.deleteTask(id);
      await getTodos();//refresh tasks list
    } catch (error) {
      console.error("Failed to delete task:", error);
    }
  }

function handleLoginSuccess() {
    setIsAuthenticated(true);
    getTodos();
  }

  function handleLogout() {
    service.logOut();
    setIsAuthenticated(false);
    setTodos([]);
  }

  // אם לא מחובר - הצג את דף ההתחברות
  if (!isAuthenticated) {
    return <Login onLoginSuccess={handleLoginSuccess} />;
  }

  // useEffect(() => {
  //   getTodos();
  // }, []);

  return (
    <section className="todoapp">
      <header className="header">
        <h1>todos</h1>
        <button 
          onClick={handleLogout} 
          style={{ 
            position: 'absolute', 
            top: '10px', 
            right: '10px', 
            padding: '8px 15px',
            backgroundColor: '#dc3545',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            zIndex: 1000, // ✅ תיקון - ודא שהכפתור מעל כל דבר אחר
            fontSize: '14px'
          }}
        >
          Logout
        </button>
        <form onSubmit={createTodo}>
          <input className="new-todo" placeholder="Well, let's take on the day" value={newTodo} onChange={(e) => setNewTodo(e.target.value)} />
        </form>
      </header>
      <section className="main" style={{ display: "block" }}>
        <ul className="todo-list">
          {todos.map(todo => {
            return (
              <li className={todo.isComplete ? "completed" : ""} key={todo.id}>
                <div className="view">
                  <input className="toggle" type="checkbox" checked={todo.isComplete} onChange={(e) => updateCompleted(todo, e.target.checked)} />
                  <label>{todo.name}</label>
                  <button className="destroy" onClick={() => deleteTodo(todo.id)}></button>
                </div>
              </li>
            );
          })}
        </ul>
      </section>
    </section >
  );
}

export default App;

