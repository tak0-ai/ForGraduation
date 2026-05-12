import urllib.request
import json
import ssl

# 创建SSL上下文，忽略证书验证
ssl_context = ssl.create_default_context()
ssl_context.check_hostname = False
ssl_context.verify_mode = ssl.CERT_NONE

base_url = "https://localhost:7061"

# 首先尝试登录获取token
login_data = {
    "email": "test@test.com",
    "password": "test123"
}

try:
    # 登录
    login_json = json.dumps(login_data).encode('utf-8')
    login_request = urllib.request.Request(
        f"{base_url}/api/auth/login",
        data=login_json,
        headers={'Content-Type': 'application/json'}
    )
    
    with urllib.request.urlopen(login_request, context=ssl_context) as response:
        response_data = response.read().decode('utf-8')
        print(f"Login response: {response_data[:200]}...")
        
        token_data = json.loads(response_data)
        token = token_data.get("token")
        print(f"Got token: {token[:20]}...")
        
        # 使用token获取行程列表
        headers = {"Authorization": f"Bearer {token}"}
        tour_request = urllib.request.Request(
            f"{base_url}/api/tourplans/me",
            headers=headers
        )
        
        with urllib.request.urlopen(tour_request, context=ssl_context) as tour_response:
            tour_data = tour_response.read().decode('utf-8')
            print(f"Tour plans response: {tour_data[:500]}...")
            
            tour_plans = json.loads(tour_data)
            print(f"Found {len(tour_plans)} tour plans")
            for plan in tour_plans:
                print(f"  - {plan.get('title', 'No title')}")
                
except Exception as e:
    print(f"Error: {e}")
    import traceback
    traceback.print_exc()