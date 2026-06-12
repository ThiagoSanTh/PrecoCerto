import { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { View } from 'react-native';
import { useLayoutProfile } from '../hooks/useLayoutProfile';
import CustomTabBar from '../components/CustomTabBar';

const TabBarPropsContext = createContext(null);

export function useTabBarPropsSetter() {
  return useContext(TabBarPropsContext);
}

function CaptureTabBarProps({ props, onCapture }) {
  useEffect(() => {
    onCapture(props);
  }, [props, props.state.index, onCapture]);
  return null;
}

export default function TabShell({ children }) {
  const { useSidebarNav } = useLayoutProfile();
  const [tabBarProps, setTabBarProps] = useState(null);
  const capture = useCallback((props) => setTabBarProps(props), []);

  const renderTabBar = useCallback(
    (props) => {
      if (useSidebarNav) {
        return <CaptureTabBarProps props={props} onCapture={capture} />;
      }
      return <CustomTabBar {...props} />;
    },
    [useSidebarNav, capture]
  );

  return (
    <TabBarPropsContext.Provider value={setTabBarProps}>
      <View style={{ flex: 1, flexDirection: useSidebarNav ? 'row' : 'column' }}>
        {useSidebarNav && tabBarProps ? <CustomTabBar {...tabBarProps} /> : null}
        <View style={{ flex: 1 }}>{children(renderTabBar)}</View>
      </View>
    </TabBarPropsContext.Provider>
  );
}
