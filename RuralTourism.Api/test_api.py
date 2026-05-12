import requests
import json

# 测试API端点
base_url = "https://localhost:7061"

# 首先尝试登录获取token
login_data = {
    "email": "test@test.com",
    "password": "test123"
}

try:
    # 登录
    response = requests.post(f"{base_url}/api/auth/login", json=login_data, verify=False)
    print(f"Login response status: {response.status_code}")
    
    if response.status_code == 200:
        token_data = response.json()
        token = token_data.get("token")
        print(f"Got token: {token[:20]}...")
        
        # 使用token获取行程列表
        headers = {"Authorization": f"Bearer {token}"}
        response = requests.get(f"{base_url}/api/tourplans/me", headers=headers, verify=False)
        print(f"Tour plans response status: {response.status_code}")
        print(f"Response: {response.text[:500]}")
        
        if response.status_code == 200:
            tour_plans = response.json()
            print(f"Found {len(tour_plans)} tour plans")
            for plan in tour_plans:
                print(f"  - {plan.get('title', 'No title')}")
    else:
        print(f"Login failed: {response.text}")
        
except Exception as e:
    print(f"Error: {e}")